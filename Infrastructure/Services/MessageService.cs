using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Hubs;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.SignalR;
using linksy_backend_api.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Domain.Interfaces.Services;

namespace linksy_backend_api.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MessageService> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ICacheService _cache; 
        private readonly INotificationService _messageNotificationService;

        public MessageService(
            IUnitOfWork unitOfWork,
            ILogger<MessageService> logger,
            IConnectionManager connectionManager,
            IHubContext<ChatHub> hubContext,
            INotificationService messageNotificationService, 
            ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _connectionManager = connectionManager;
            _hubContext = hubContext;
            _messageNotificationService = messageNotificationService;
            _cache = cache;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET
        // ─────────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<MessageResponse>> GetMessagesAsync(Guid userId, Guid chatroomId, int page = 1, int pageSize = 50)
        { // Chỉ cache page 1 (tin nhắn mới nhất) — các page cũ không cần realtime
            if (page == 1)
            {
                var cacheKey = CacheKeys.Messages(chatroomId, page);
                var cached = await _cache.GetAsync<List<MessageResponse>>(cacheKey);
                if (cached is not null)
                {
                    // Cập nhật IsOwn theo userId hiện tại (không lưu trong cache)
                    cached.ForEach(m => m.IsOwn = m.SenderId == userId);
                    return cached;
                }
            }
            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(
                rm => rm.ChatroomId == chatroomId && rm.UserId == userId && rm.LeftAt == null);

            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không có quyền xem tin nhắn này.");

            // Dùng MessageRepository đã tách
            var messages = await _unitOfWork.MessageRepository.GetChatroomMessagesAsync(chatroomId, page, pageSize);

            var result = new List<MessageResponse>();
            foreach (var message in messages)
                result.Add(await MessageMapper.ToResponseAsync(message, _unitOfWork, userId));

            result.Reverse(); // tin cũ ở trên, tin mới ở dưới
            if (page == 1)
                await _cache.SetAsync(CacheKeys.Messages(chatroomId, 1), result, CacheKeys.ShortTtl);

            return result;
        }

        public async Task<List<MessageResponse>> GetRepliesAsync(Guid messageId)
        {
            var replies = await _unitOfWork.MessageRepository.GetRepliesAsync(messageId);
            var result = new List<MessageResponse>();
            foreach (var r in replies)
                result.Add(await MessageMapper.ToResponseAsync(r, _unitOfWork));
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEND
        // ─────────────────────────────────────────────────────────────────────

        public async Task<MessageResponse> SendMessageAsync(Guid userId, SendMessageRequest messageDto)
        {
            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(rm =>
                rm.ChatroomId == messageDto.ChatroomId &&
                rm.UserId == userId &&
                rm.LeftAt == null);

            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của phòng chat này.");

            var canSend = await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, messageDto.ChatroomId, PermissionType.CanSendMessages);
            if (!canSend)
                throw new UnauthorizedAccessException("Bạn không có quyền gửi tin nhắn trong phòng chat này.");

            if (messageDto.ParentMessageId.HasValue)
            {
                var parent = await _unitOfWork.Messages.GetByIdAsync(messageDto.ParentMessageId.Value)
                    ?? throw new ArgumentException("Không tìm thấy tin nhắn gốc.");
                if (parent.ChatroomId != messageDto.ChatroomId)
                    throw new ArgumentException("Tin nhắn gốc không thuộc phòng chat này.");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    ChatroomId = messageDto.ChatroomId,
                    SenderId = userId,
                    MessageText = messageDto.MessageText,
                    MessageType = messageDto.MessageType,
                    ParentMessageId = messageDto.ParentMessageId,
                    SentAt = DateTime.UtcNow,
                    IsEdited = false,
                    IsDeleted = false
                };
                await _unitOfWork.Messages.AddAsync(message);

                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(messageDto.ChatroomId);
                if (chatroom != null)
                {
                    chatroom.LastMessageId = message.MessageId;
                    chatroom.LastMessageAt = message.SentAt;
                    chatroom.LastActivityAt = message.SentAt;
                    _unitOfWork.Chatrooms.Update(chatroom);
                }

                await CreateMessageNotificationsAsync(message, userId);
                await _unitOfWork.CommitTransactionAsync();

                var response = await MessageMapper.ToResponseAsync(message, _unitOfWork, userId);
                await _cache.RemoveAsync(CacheKeys.Messages(messageDto.ChatroomId, 1));

                return response;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error sending message");
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // EDIT / DELETE / READ
        // ─────────────────────────────────────────────────────────────────────

        public async Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, string newText)
        {
            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            if (message.SenderId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền sửa tin nhắn này.");

            if (message.IsDeleted == true)
                throw new InvalidOperationException("Không thể sửa tin nhắn đã xóa.");

            message.MessageText = newText;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;
            _unitOfWork.Messages.Update(message);
            await _unitOfWork.SaveChangesAsync();

            await _hubContext.Clients.Group(message.ChatroomId.ToString())
                .SendAsync("MessageEdited", new { MessageId = messageId, NewText = newText, EditedAt = message.EditedAt });

            return await MessageMapper.ToResponseAsync(message, _unitOfWork, userId);
        }

        public async Task DeleteMessageAsync(Guid userId, Guid messageId)
        {
            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            bool isOwner = message.SenderId == userId;
            bool canDelete = isOwner || await _unitOfWork.MemberPermissionRepository
                .HasPermissionAsync(userId, message.ChatroomId, PermissionType.CanDeleteMessages);

            if (!canDelete)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa tin nhắn này.");

            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            _unitOfWork.Messages.Update(message);
            await _unitOfWork.SaveChangesAsync();

            await _hubContext.Clients.Group(message.ChatroomId.ToString())
                .SendAsync("MessageDeleted", new { MessageId = messageId, DeletedBy = userId });
        }

        public async Task MarkMessageAsReadAsync(Guid userId, Guid chatroomId, Guid messageId)
        {
            var member = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new InvalidOperationException("Bạn không phải thành viên phòng chat này.");

            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            member.LastReadAt = message.SentAt;
            _unitOfWork.ChatroomMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // NOTIFICATIONS
        // ─────────────────────────────────────────────────────────────────────

        public async Task CreateMessageNotificationsAsync(Message message, Guid senderId)
        {
            try
            {
                var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(message.ChatroomId);

                if (sender == null || chatroom == null)
                {
                    _logger.LogWarning("Sender or chatroom not found. SenderId: {SenderId}, ChatroomId: {ChatroomId}", senderId, message.ChatroomId);
                    return;
                }

                var recipientIds = await _unitOfWork.ChatroomMembers.Query()
                    .Where(rm => rm.ChatroomId == message.ChatroomId && rm.UserId != senderId && rm.LeftAt == null)
                    .Select(rm => rm.UserId)
                    .ToListAsync();

                await _messageNotificationService.NotifyNewMessageAsync(message, sender, chatroom, recipientIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message notifications");
                throw;
            }
        }

    }
}