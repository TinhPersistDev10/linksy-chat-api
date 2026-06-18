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
using linksy_backend_api.Core.DTOs.Requests.Notifications;

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
        // FIX #1 (Cache stampede): dùng SemaphoreSlim per-key để tránh nhiều request
        // cùng miss cache và gọi DB đồng thời
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim>
            _cacheLocks = new();
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

            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(
                rm => rm.ChatroomId == chatroomId && rm.UserId == userId && rm.LeftAt == null);

            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không có quyền xem tin nhắn này.");
            // if (page == 1)
            // {
            //     var cacheKey = CacheKeys.Messages(chatroomId, page, pageSize);
            //     var cached = await _cache.GetAsync<List<MessageResponse>>(cacheKey);
            //     if (cached is not null)
            //     {
            //         // Cập nhật IsOwn theo userId hiện tại (không lưu trong cache)
            //         cached.ForEach(m => m.IsOwn = m.SenderId == userId);
            //         return cached;
            //     }
            // }

            // Dùng MessageRepository đã tách
            var messages = await _unitOfWork.MessageRepository.GetChatroomMessagesAsync(chatroomId, page, pageSize);

            var result = new List<MessageResponse>();
            foreach (var message in messages)
                result.Add(await MessageMapper.ToResponseAsync(message, _unitOfWork, userId));

            result.Reverse(); // tin cũ ở trên, tin mới ở dưới
            // if (page == 1)
            //     await _cache.SetAsync(
            //         CacheKeys.Messages(chatroomId, 1, pageSize),
            //          result,
            //           page == 1 ? CacheKeys.ShortTtl : CacheKeys.LongTtl);

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
            Message message;
            try
            {
                message = new Message
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

                var recipientIds = await _unitOfWork.ChatroomMemberRepository
                    .GetActiveMemberIdsExceptAsync(messageDto.ChatroomId, userId);

                await _unitOfWork.MessageDeliveryRepository.CreateDeliveriesForMembersAsync(
                    message.MessageId,
                    recipientIds);

                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(messageDto.ChatroomId);
                if (chatroom != null)
                {
                    chatroom.LastMessageId = message.MessageId;
                    chatroom.LastMessageAt = message.SentAt;
                    chatroom.LastActivityAt = message.SentAt;
                    _unitOfWork.Chatrooms.Update(chatroom);
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error sending message");
                throw;
            }
            try
            {
                await CreateMessageNotificationsAsync(message, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification failed for MessageId={MessageId}, message was saved successfully",
                    message.MessageId);
            }
            var response = await MessageMapper.ToResponseAsync(message, _unitOfWork, userId);
            // await _cache.RemoveAsync(CacheKeys.Messages(messageDto.ChatroomId, 1, 50));
            try
            {
                await _hubContext.Clients
                .Group(messageDto.ChatroomId.ToString())
                .SendAsync("ReceiveMessage", response);
            }
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "SignalR broadcast failed for MessageId={MessageId}", message.MessageId);
            }

            return response;

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
            await _unitOfWork.BeginTransactionAsync();
            try
            {

                message.MessageText = newText;
                message.IsEdited = true;
                message.EditedAt = DateTime.UtcNow;
                _unitOfWork.Messages.Update(message);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (System.Exception ex)
            {

                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error editing MessageId={MessageId}", messageId);
            }

            await _cache.RemoveAsync(CacheKeys.Messages(message.ChatroomId, 1, 50));
            try
            {
                await _hubContext.Clients.Group(message.ChatroomId.ToString())
                    .SendAsync("MessageEdited",
                     new
                     {
                         MessageId = messageId,
                         NewText = newText,
                         EditedAt = message.EditedAt
                     });

            }
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "SignalR broadcast failed for MessageEdited MessageId={MessageId}", messageId);
                throw;
            }

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
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                message.IsDeleted = true;
                message.DeletedAt = DateTime.UtcNow;

                _unitOfWork.Messages.Update(message);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

            }
            catch (System.Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting MessageId={MessageId}", messageId);
            }

            await _cache.RemoveAsync(CacheKeys.Messages(message.ChatroomId, 1, 50));
            try
            {

                await _hubContext.Clients.Group(message.ChatroomId.ToString())
                .SendAsync("MessageDeleted", new { MessageId = messageId, DeletedBy = userId });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "SignalR broadcast failed for MessageDeleted MessageId={MessageId}", messageId);
            }
        }

        public async Task MarkMessageAsReadAsync(Guid userId, Guid chatroomId, Guid messageId)
        {
            var member = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new InvalidOperationException("Bạn không phải thành viên phòng chat này.");

            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            if (message.ChatroomId != chatroomId)
                throw new InvalidOperationException("Tin nhắn không thuộc chatroom này.");

            member.LastReadAt = message.SentAt;
            _unitOfWork.ChatroomMembers.Update(member);

            await _unitOfWork.MessageDeliveryRepository.MarkAsReadAsync(messageId, userId);

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

                var recipientIds = await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberIdsExceptAsync(message.ChatroomId, senderId);

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