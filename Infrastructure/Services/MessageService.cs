using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Hubs;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class MessageService : IMessageService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MessageService> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly INotificationService _messageNotificationService;

        public MessageService(IUnitOfWork unitOfWork, ILogger<MessageService> logger, IConnectionManager connectionManager, IHubContext<ChatHub> hubContext, INotificationService messageNotificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _connectionManager = connectionManager;
            _hubContext = hubContext;
            _messageNotificationService = messageNotificationService;
        }

        #region Get Messages
        public async Task<IEnumerable<MessageResponse>> GetMessagesAsync(Guid userId, Guid chatroomId, int page = 1, int pageSize = 50)
        {
            // Kiểm tra user có trong chatroom không
            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(
                rm => rm.ChatroomId == chatroomId && rm.UserId == userId);

            if (!isMember)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem tin nhắn này");
            }

            var messages = await _unitOfWork.Messages.Query()
                .Where(m => m.ChatroomId == chatroomId && (m.IsDeleted == null || m.IsDeleted == false))
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(m => m.Sender)
                .ToListAsync();

            var result = new List<MessageResponse>();
            foreach (var message in messages)
            {
                var dto = await MapToMessageResponseAsync(message);
                dto.IsOwn = message.SenderId == userId;
                result.Add(dto);
            }

            // Reverse để tin cũ ở trên, tin mới ở dưới
            result.Reverse();
            return result;
        }
        #endregion

        public async Task<MessageResponse> SendMessageAsync(Guid userId, SendMessageRequest messageDto)
        {
            // Kiểm tra user có trong chatroom không
            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(rm =>
            rm.ChatroomId == messageDto.ChatroomId &&
            rm.UserId == userId &&
            rm.LeftAt == null);

            if (!isMember)
            {
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của phòng chat này.");
            }

            var canSend = await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, messageDto.ChatroomId, "CanSendMessages");

            if (!canSend)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền gửi tin nhắn trong phòng chat này.");
            }

            if (messageDto.ParentMessageId.HasValue)
            {
                var parentMessage = await _unitOfWork.Messages.GetByIdAsync(messageDto.ParentMessageId.Value);
                if (parentMessage == null)
                {
                    throw new ArgumentException("Không tìm thấy tin nhắn gốc.");
                }
                if (parentMessage.ChatroomId != messageDto.ChatroomId)
                {
                    throw new ArgumentException("Tin nhắn gốc không thuộc phòng chat này.");
                }
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
                
                var sender = await _unitOfWork.Users.GetByIdAsync(userId);

                var messageResponse = await MapToMessageResponseAsync(message);
                messageResponse.IsOwn = true;
                return messageResponse;
            }
            catch (System.Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error sending message");
                throw;
            }


        }
        public Task<List<MessageResponse>> GetRepliesAsync(Guid messageId)
        {
            throw new NotImplementedException();
        }
        public Task MarkMessageAsReadAsync(Guid userId, Guid chatroomId, Guid messageId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteMessageAsync(Guid userId, Guid messageId)
        {
            throw new NotImplementedException();
        }

        public Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, string newText)
        {
            throw new NotImplementedException();
        }
        #region Realtime Notifications
        public async Task CreateMessageNotificationsAsync(Message message, Guid senderId)
        {
            try
            {

                var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(message.ChatroomId);

                if (sender == null || chatroom == null)
                {
                    _logger.LogWarning("Sender or chatroom not found for message notification creation. SenderId: {SenderId}, ChatroomId: {ChatroomId}", senderId, message.ChatroomId);
                    return;
                }


                var recipientIds = await _unitOfWork.ChatroomMembers.Query()
                                    .Where(rm => rm.ChatroomId == message.ChatroomId && rm.UserId != senderId && rm.LeftAt == null)
                                    .Select(rm => rm.UserId)
                                    .ToListAsync();
                await _messageNotificationService.NotifyNewMessageAsync(message, sender, chatroom, recipientIds);

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating message notifications");
                throw;
            }

        }

        #endregion
        #region Map To Message Response
        public async Task<MessageResponse> MapToMessageResponseAsync(Message message)
        {
            var sender = message.Sender ?? await _unitOfWork.Users.GetByIdAsync(message.SenderId ?? Guid.Empty);
            MessageResponse? parentMessageDto = null;
            if (message.ParentMessageId.HasValue)
            {
                var parentMessage = await _unitOfWork.Messages.Query()
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.MessageId == message.ParentMessageId.Value);

                if (parentMessage != null)
                {
                    parentMessageDto = new MessageResponse
                    {
                        MessageId = parentMessage.MessageId,
                        SenderId = parentMessage.SenderId ?? Guid.Empty,
                        SenderUsername = parentMessage.Sender?.Username ?? string.Empty,
                        MessageText = parentMessage.MessageText ?? string.Empty,
                        SentAt = parentMessage.SentAt ?? DateTime.UtcNow
                    };
                }
            }
            return new MessageResponse
            {
                MessageId = message.MessageId,
                ChatroomId = message.ChatroomId,
                SenderId = message.SenderId ?? Guid.Empty,
                SenderUsername = sender?.Username ?? string.Empty,
                SenderAvatar = DefaultAvatarHelper.GetAvatarOrDefault(sender?.Avatar, sender?.UserId),
                MessageType = message.MessageType,
                MessageText = message.MessageText ?? string.Empty,
                ParentMessageId = message.ParentMessageId,
                IsEdited = message.IsEdited ?? false,
                IsDeleted = message.IsDeleted ?? false,
                SentAt = message.SentAt ?? DateTime.UtcNow,
                EditedAt = message.EditedAt
            };
        }

        #endregion
    }

}