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
using linksy_backend_api.Domain.Events.Messages;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.DTOs.Responses.MessageAttachment;
using linksy_backend_api.Domain.DTOs.Responses.Calls;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Domain.DTOs.Responses.Reactions;
using System.Text.Json;
namespace linksy_backend_api.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MessageService> _logger;
        private readonly IFileService _fileService;
        private readonly IChatroomAccessService _chatroomAccessService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ICacheService _cache;
        private readonly INotificationService _messageNotificationService;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim>
            _cacheLocks = new();
        public MessageService(
            IUnitOfWork unitOfWork,
            ILogger<MessageService> logger,
            IFileService fileService,
            IChatroomAccessService chatroomAccessService,
            IHubContext<ChatHub> hubContext,
            INotificationService messageNotificationService,
            ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _fileService = fileService;
            _chatroomAccessService = chatroomAccessService;
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

            var messages = await _unitOfWork.MessageRepository.GetChatroomMessagesAsync(chatroomId, page, pageSize);
            // Repo trả newest-first; đảo thành oldest→newest cho UI.
            messages.Reverse();
            return await MapMessagesWithDeliveriesAsync(messages, userId);
        }

        public async Task<(List<MessageResponse> Messages, bool HasMoreBefore)> GetMessagesAroundAsync(
            Guid userId,
            Guid chatroomId,
            Guid messageId,
            int beforeCount = 20,
            int afterCount = 15)
        {
            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(
                rm => rm.ChatroomId == chatroomId && rm.UserId == userId && rm.LeftAt == null);

            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không có quyền xem tin nhắn này.");

            var (messages, hasMoreBefore) = await _unitOfWork.MessageRepository
                .GetMessagesAroundAsync(chatroomId, messageId, beforeCount, afterCount);

            if (messages.Count == 0)
                throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            return (await MapMessagesWithDeliveriesAsync(messages, userId), hasMoreBefore);
        }

        public async Task<(List<MessageResponse> Messages, bool HasMore)> GetMessagesBeforeAsync(
            Guid userId,
            Guid chatroomId,
            Guid beforeMessageId,
            int pageSize = 50)
        {
            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(
                rm => rm.ChatroomId == chatroomId && rm.UserId == userId && rm.LeftAt == null);

            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không có quyền xem tin nhắn này.");

            var (messages, hasMore) = await _unitOfWork.MessageRepository
                .GetMessagesBeforeAsync(chatroomId, beforeMessageId, pageSize);

            return (await MapMessagesWithDeliveriesAsync(messages, userId), hasMore);
        }

        private async Task<List<MessageResponse>> MapMessagesWithDeliveriesAsync(
            List<Message> messages,
            Guid userId)
        {
            var messageIds = messages.Select(message => message.MessageId).ToArray();
            var deliveries = messageIds.Length == 0
                ? new List<MessageDelivery>()
                : await _unitOfWork.MessageDeliveryRepository.GetByMessageIdsAsync(messageIds);
            var deliveriesByMessage = deliveries
                .GroupBy(delivery => delivery.MessageId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var reactionsByMessage = await LoadReactionsByMessageIdsAsync(messageIds, userId);

            var result = new List<MessageResponse>(messages.Count);
            foreach (var message in messages)
            {
                var response = await MessageMapper.ToResponseAsync(message, _unitOfWork, userId);
                deliveriesByMessage.TryGetValue(message.MessageId, out var messageDeliveries);
                ApplyDeliverySummary(response, messageDeliveries ?? new());
                response.Reactions = reactionsByMessage.TryGetValue(message.MessageId, out var reactions)
                    ? reactions
                    : new List<ReactionSummaryResponse>();
                result.Add(response);
            }

            return result;
        }

        public async Task<List<MessageResponse>> GetRepliesAsync(Guid userId, Guid messageId)
        {
            var parent = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(member =>
                member.ChatroomId == parent.ChatroomId &&
                member.UserId == userId &&
                member.LeftAt == null
            );

            if (!isMember)
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền xem các tin nhắn trả lời."
                );

            var replies = await _unitOfWork.MessageRepository
                .GetRepliesAsync(messageId);

            var result = new List<MessageResponse>();

            foreach (var reply in replies)
            {
                result.Add(await MessageMapper.ToResponseAsync(
                    reply,
                    _unitOfWork,
                    userId
                ));
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEND
        // ─────────────────────────────────────────────────────────────────────

        public async Task<MessageResponse> SendMessageAsync(Guid userId, SendMessageRequest messageDto)
        {
            messageDto.MessageType = messageDto.MessageType.Trim().ToLowerInvariant();
            messageDto.MessageText = messageDto.MessageText?.Trim() ?? string.Empty;

            var hasText = !string.IsNullOrWhiteSpace(messageDto.MessageText);
            var hasAttachments = messageDto.Attachments is not null && messageDto.Attachments.Any();
            var allowedMessageTypes = new[] { "text", "image", "video", "file", "audio" };
            if (messageDto.MessageType == "text" && !hasText)
                throw new ArgumentException("Nội dung tin nhắn không được để trống.");

            if (messageDto.MessageType != "text" && !hasAttachments)
                throw new ArgumentException("Attachment is required.");
            if (!allowedMessageTypes.Contains(messageDto.MessageType))
                throw new ArgumentException("MessageType không hợp lệ.");
            if (messageDto.MessageText.Length > 5000)
                throw new ArgumentException(
                    "Tin nhắn không được vượt quá 5000 ký tự."
                );
            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(rm =>
                rm.ChatroomId == messageDto.ChatroomId &&
                rm.UserId == userId &&
                rm.LeftAt == null);

            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của phòng chat này.");

            var canSend = await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, messageDto.ChatroomId, PermissionType.CanSendMessages);
            if (!canSend)
                throw new UnauthorizedAccessException("Bạn không có quyền gửi tin nhắn trong phòng chat này.");
            await _chatroomAccessService.EnsurePermissionAsync(
                messageDto.ChatroomId,
                userId,
                PermissionType.CanSendMessages);
            if (messageDto.MessageType is "image" or "video")
            {
                await _chatroomAccessService.EnsurePermissionAsync(
                    messageDto.ChatroomId,
                    userId,
                    PermissionType.CanSendMedia);
            }
            else if (messageDto.MessageType == "file")
            {
                await _chatroomAccessService.EnsurePermissionAsync(
                    messageDto.ChatroomId,
                    userId,
                    PermissionType.CanSendFiles);
            }
            else if (messageDto.MessageType == "audio")
            {
                await _chatroomAccessService.EnsurePermissionAsync(
                    messageDto.ChatroomId,
                    userId,
                    PermissionType.CanSendVoice);
            }
            if (messageDto.ParentMessageId.HasValue)
            {
                var parent = await _unitOfWork.Messages
                    .GetByIdAsync(messageDto.ParentMessageId.Value)
                    ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn gốc.");

                if (parent.ChatroomId != messageDto.ChatroomId)
                    throw new ArgumentException(
                        "Tin nhắn gốc không thuộc phòng chat này."
                    );

                if (parent.IsDeleted == true)
                    throw new InvalidOperationException(
                        "Không thể trả lời tin nhắn đã bị xóa."
                    );
            }

            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(messageDto.ChatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");

            var validatedMentions = await ValidateMentionsAsync(
                chatroom,
                userId,
                messageDto.Mentions);

            await _unitOfWork.BeginTransactionAsync();
            Message message;
            try
            {
                message = await PersistMessageRecordAsync(
                    messageDto.ChatroomId,
                    userId,
                    messageDto.MessageType,
                    messageDto.MessageText,
                    messageDto.ParentMessageId,
                    messageDto.Attachments,
                    validatedMentions);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Database transaction failed while sending message. ChatroomId={ChatroomId}, UserId={UserId}, ParentMessageId={ParentMessageId}",
                    messageDto.ChatroomId,
                    userId,
                    messageDto.ParentMessageId
                );
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
            response.Reactions ??= new List<ReactionSummaryResponse>();
            var newMessageDeliveries = await _unitOfWork.MessageDeliveryRepository
                .GetByMessageAsync(message.MessageId);
            ApplyDeliverySummary(response, newMessageDeliveries);
            // await _cache.RemoveAsync(CacheKeys.Messages(messageDto.ChatroomId, 1, 50));
            await InvalidateChatroomListCacheAsync(
                newMessageDeliveries.Select(delivery => delivery.UserId).Append(userId));
            await BroadcastReceiveMessageAsync(response);

            return response;

        }

        /// <summary>
        /// Inserts a Message row plus its attachments/deliveries/chatroom bookkeeping.
        /// Shared by SendMessageAsync (user-authored) and CreateCallLogMessageAsync
        /// (system-generated) so both stay consistent without duplicating the
        /// persistence steps. Caller owns the transaction.
        /// </summary>
        private const int MaxMentionsPerMessage = 20;

        private async Task<List<Guid>> ValidateMentionsAsync(
            Chatroom chatroom,
            Guid senderId,
            List<Guid>? mentions)
        {
            if (mentions is null || mentions.Count == 0)
                return [];

            if (!string.Equals(chatroom.RoomType, "group", StringComparison.OrdinalIgnoreCase))
                return [];

            var distinctMentions = mentions
                .Where(id => id != Guid.Empty && id != senderId)
                .Distinct()
                .ToList();

            if (distinctMentions.Count == 0)
                return [];

            if (distinctMentions.Count > MaxMentionsPerMessage)
                throw new ArgumentException($"Mỗi tin nhắn chỉ được tag tối đa {MaxMentionsPerMessage} người.");

            var activeMemberIds = (await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberIdsExceptAsync(chatroom.ChatroomId, Guid.Empty))
                .ToHashSet();

            var invalid = distinctMentions.Where(id => !activeMemberIds.Contains(id)).ToList();
            if (invalid.Count > 0)
                throw new ArgumentException("Chỉ có thể tag thành viên đang hoạt động trong nhóm.");

            return distinctMentions;
        }

        private async Task<Message> PersistMessageRecordAsync(
            Guid chatroomId,
            Guid? senderId,
            string messageType,
            string messageText,
            Guid? parentMessageId,
            IEnumerable<Domain.DTOs.Requests.SendMessageAttachmentRequest>? attachments,
            IReadOnlyCollection<Guid>? mentionedUserIds = null)
        {
            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                ChatroomId = chatroomId,
                SenderId = senderId,
                MessageText = messageText,
                MessageType = messageType,
                ParentMessageId = parentMessageId,
                SentAt = DateTime.UtcNow,
                IsEdited = false,
                IsDeleted = false
            };
            await _unitOfWork.Messages.AddAsync(message);

            if (attachments is not null)
            {
                foreach (var item in attachments)
                {
                    var attachment = new MessageAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        MessageId = message.MessageId,
                        AttachmentType = item.AttachmentType,
                        FileName = item.FileName,
                        CdnUrl = item.CdnUrl,
                        FileSize = item.FileSize,
                        MimeType = item.MimeType,
                        ThumbnailUrl = item.ThumbnailUrl,
                        Width = item.Width,
                        Height = item.Height,
                        DurationMs = item.DurationMs,
                        UploadedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.MessageAttachments.AddAsync(attachment);
                }
            }

            if (mentionedUserIds is not null)
            {
                foreach (var mentionedUserId in mentionedUserIds)
                {
                    await _unitOfWork.MessageMentions.AddAsync(new MessageMention
                    {
                        MessageId = message.MessageId,
                        MentionedUserId = mentionedUserId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var recipientIds = await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberIdsExceptAsync(chatroomId, senderId ?? Guid.Empty);

            await _unitOfWork.MessageDeliveryRepository.CreateDeliveriesForMembersAsync(
                message.MessageId,
                recipientIds);

            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
            if (chatroom != null)
            {
                chatroom.LastMessageId = message.MessageId;
                chatroom.LastMessageAt = message.SentAt;
                chatroom.LastActivityAt = message.SentAt;
                _unitOfWork.Chatrooms.Update(chatroom);
            }

            return message;
        }

        private static readonly JsonSerializerOptions CallLogPayloadJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task<MessageResponse> CreateCallLogMessageAsync(
            Guid chatroomId,
            Guid callerId,
            Guid callLogId,
            string callType,
            string callStatus,
            int durationSec,
            DateTime startedAt,
            DateTime? endedAt)
        {
            var caller = await _unitOfWork.Users.GetByIdAsync(callerId);
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
            var payload = JsonSerializer.Serialize(
                new CallLogMessagePayload(
                    callLogId,
                    callType,
                    callStatus,
                    Math.Max(0, durationSec),
                    startedAt,
                    endedAt,
                    string.Equals(chatroom?.RoomType, "group", StringComparison.OrdinalIgnoreCase),
                    callerId,
                    caller?.Fullname ?? caller?.Username,
                    chatroom?.RoomName),
                CallLogPayloadJsonOptions);

            await _unitOfWork.BeginTransactionAsync();
            Message message;
            try
            {
                message = await PersistMessageRecordAsync(
                    chatroomId,
                    callerId,
                    "call_log",
                    payload,
                    parentMessageId: null,
                    attachments: null);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Database transaction failed while creating call log message. ChatroomId={ChatroomId}, CallLogId={CallLogId}",
                    chatroomId,
                    callLogId);
                throw;
            }

            var response = await MessageMapper.ToResponseAsync(message, _unitOfWork, callerId);
            var deliveries = await _unitOfWork.MessageDeliveryRepository
                .GetByMessageAsync(message.MessageId);
            ApplyDeliverySummary(response, deliveries);
            await InvalidateChatroomListCacheAsync(
                deliveries.Select(delivery => delivery.UserId).Append(callerId));
            await BroadcastReceiveMessageAsync(response);

            return response;
        }

        private async Task BroadcastReceiveMessageAsync(MessageResponse response)
        {
            try
            {
                await _hubContext.Clients
                    .Group(response.ChatroomId.ToString())
                    .SendAsync("ReceiveMessage", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR broadcast failed for MessageId={MessageId}", response.MessageId);
            }
        }

        public async Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, string newText)
        {
            newText = newText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(newText))
                throw new ArgumentException("Nội dung tin nhắn không được để trống.");
            if (newText.Length > 5000)
                throw new ArgumentException("Tin nhắn không được vượt quá 5000 ký tự.");

            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            if (message.SenderId != userId)
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền sửa tin nhắn này."
                );

            if (message.IsDeleted == true)
                throw new InvalidOperationException(
                    "Không thể sửa tin nhắn đã xóa."
                );

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
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(
                    ex,
                    "Database transaction failed while editing MessageId={MessageId}, ChatroomId={ChatroomId}, UserId={UserId}",
                    message.MessageId,
                    message.ChatroomId,
                    userId
                );

                throw;
            }
            _logger.LogInformation(
                "Message edited successfully. MessageId={MessageId}, ChatroomId={ChatroomId}, EditedBy={EditedBy}, EditedAt={EditedAt}",
                message.MessageId,
                message.ChatroomId,
                userId,
                message.EditedAt
            );

            try
            {
                await _cache.RemoveAsync(
                    CacheKeys.Messages(message.ChatroomId, 1, 50)
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Cache invalidation failed after editing MessageId={MessageId}, ChatroomId={ChatroomId}, UserId={UserId}",
                    message.MessageId,
                    message.ChatroomId,
                    userId
                );
            }

            var editedEvent = new MessageEditedEvent
            {
                MessageId = message.MessageId,
                ChatroomId = message.ChatroomId,
                MessageText = message.MessageText ?? string.Empty,
                IsEdited = message.IsEdited ?? false,
                EditedAt = message.EditedAt,
                EditedBy = userId
            };

            try
            {
                await _hubContext.Clients
                    .Group(message.ChatroomId.ToString())
                    .SendAsync("MessageEdited", editedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SignalR broadcast failed for MessageEdited. MessageId={MessageId}, ChatroomId={ChatroomId}, EditedBy={EditedBy}",
                    message.MessageId,
                    message.ChatroomId,
                    userId
                );
            }

            return await MessageMapper.ToResponseAsync(
                message,
                _unitOfWork,
                userId
            );
        }

        public async Task DeleteMessageAsync(Guid userId, Guid messageId)
        {
            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");
            if (message.IsDeleted == true)
                throw new InvalidOperationException("Tin nhắn đã bị xóa.");
            bool isOwner = message.SenderId == userId;
            bool canDelete = isOwner || await _unitOfWork.MemberPermissionRepository
                .HasPermissionAsync(userId, message.ChatroomId, PermissionType.CanDeleteMessages);

            if (!canDelete)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa tin nhắn này.");
            await _unitOfWork.BeginTransactionAsync();

            var existingPin = await _unitOfWork.PinnedMessages.Query()
                .FirstOrDefaultAsync(p => p.MessageId == messageId);

            try
            {
                message.IsDeleted = true;
                message.DeletedAt = DateTime.UtcNow;

                _unitOfWork.Messages.Update(message);
                if (existingPin != null)
                    _unitOfWork.PinnedMessages.Remove(existingPin);

                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(
                    ex,
                    "Database transaction failed while deleting MessageId={MessageId}, ChatroomId={ChatroomId}, UserId={UserId}",
                    message.MessageId,
                    message.ChatroomId,
                    userId
                );

                throw;
            }
            _logger.LogInformation(
                "Message deleted successfully. MessageId={MessageId}, ChatroomId={ChatroomId}, DeletedBy={DeletedBy}, DeletedAt={DeletedAt}",
                message.MessageId,
                message.ChatroomId,
                userId,
                message.DeletedAt
            );
            try
            {
                await _cache.RemoveAsync(
                    CacheKeys.Messages(message.ChatroomId, 1, 50)
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Cache invalidation failed after deleting MessageId={MessageId}, ChatroomId={ChatroomId}, UserId={UserId}",
                    message.MessageId,
                    message.ChatroomId,
                    userId
                );
            }
            var deletedEvent = new MessageDeletedEvent
            {
                MessageId = message.MessageId,
                ChatroomId = message.ChatroomId,
                DeletedBy = userId,
                DeletedAt = message.DeletedAt!.Value
            };

            try
            {
                await _hubContext.Clients
                    .Group(message.ChatroomId.ToString())
                    .SendAsync("MessageDeleted", deletedEvent);

                if (existingPin != null)
                {
                    await _hubContext.Clients
                        .Group(message.ChatroomId.ToString())
                        .SendAsync("MessageUnpinned", new MessageUnpinnedEvent
                        {
                            ChatroomId = message.ChatroomId,
                            MessageId = messageId,
                            UnpinnedBy = userId
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SignalR broadcast failed for MessageDeleted. MessageId={MessageId}, ChatroomId={ChatroomId}, DeletedBy={DeletedBy}",
                    message.MessageId,
                    message.ChatroomId,
                    userId
                );
            }
        }
        public async Task<UploadAttachmentResponse> UploadAttachmentAsync(
     Guid userId,
     Guid chatroomId,
     IFormFile file,
     string attachmentType)
        {
            attachmentType = attachmentType.Trim().ToLowerInvariant();

            await _chatroomAccessService.EnsureMemberAsync(chatroomId, userId);

            if (attachmentType is "image" or "video")
            {
                await _chatroomAccessService.EnsurePermissionAsync(
                    chatroomId,
                    userId,
                    PermissionType.CanSendMedia);
            }
            else if (attachmentType == "file")
            {
                await _chatroomAccessService.EnsurePermissionAsync(
                    chatroomId,
                    userId,
                    PermissionType.CanSendFiles);
            }
            else
            {
                throw new ArgumentException("Invalid attachment type.");
            }

            return await _fileService.UploadMessageAttachmentAsync(
                file,
                attachmentType,
                chatroomId);
        }
        public async Task MarkMessageAsDeliveredAsync(Guid userId, Guid messageId)
        {
            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");
            var delivery = await _unitOfWork.MessageDeliveryRepository
                .GetByMessageAndUserAsync(messageId, userId)
                ?? throw new UnauthorizedAccessException("Không có delivery cho người dùng.");

            await _unitOfWork.MessageDeliveryRepository.MarkAsDeliveredAsync(messageId, userId);
            await _unitOfWork.SaveChangesAsync();
            var deliveries = await _unitOfWork.MessageDeliveryRepository.GetByMessageAsync(messageId);
            var deliveredEvent = new MessageDeliveredEvent
            {
                ChatroomId = message.ChatroomId,
                DeliveredBy = userId,
                DeliveredAt = delivery.DeliveredAt ?? DateTime.UtcNow,
                Message = BuildDeliverySummary(messageId, deliveries)
            };
            await BroadcastMessageDeliveredAsync(deliveredEvent);
        }

        public async Task MarkMessageAsReadAsync(Guid userId, Guid chatroomId, Guid messageId)
        {
            var member = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new InvalidOperationException("Bạn không phải thành viên phòng chat này.");

            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            if (message.ChatroomId != chatroomId)
                throw new InvalidOperationException("Tin nhắn không thuộc chatroom này.");

            if (!member.LastReadAt.HasValue || member.LastReadAt < message.SentAt)
            {
                member.LastReadAt = message.SentAt;
            }
            _unitOfWork.ChatroomMembers.Update(member);

            await _unitOfWork.MessageDeliveryRepository.MarkAsReadAsync(messageId, userId);

            await _unitOfWork.SaveChangesAsync();
            await InvalidateChatroomListCacheAsync(userId);
            var deliveries = await _unitOfWork.MessageDeliveryRepository.GetByMessageAsync(messageId);
            var readAt = deliveries.FirstOrDefault(delivery => delivery.UserId == userId)?.ReadAt
                ?? DateTime.UtcNow;
            var readEvent = new MessageReadEvent
            {
                ChatroomId = chatroomId,
                ReadBy = userId,
                ReadAt = readAt,
                Message = BuildDeliverySummary(messageId, deliveries)
            };
            await BroadcastMessageReadAsync(readEvent);
        }

        // ─────────────────────────────────────────────────────────────────────
        // NOTIFICATIONS
        // ─────────────────────────────────────────────────────────────────────

        public async Task MarkAllMessagesAsReadAsync(Guid userId, Guid chatroomId)
        {
            var member = await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Not a chatroom member.");
            var readAt = DateTime.UtcNow;
            var messageIds = await _unitOfWork.MessageDeliveryRepository
                .MarkAllAsReadAsync(chatroomId, userId, readAt);
            member.LastReadAt = readAt;
            _unitOfWork.ChatroomMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();
            await InvalidateChatroomListCacheAsync(userId);
            await BroadcastAllMessagesReadAsync(chatroomId, userId, readAt, messageIds);
        }

        private Task InvalidateChatroomListCacheAsync(Guid userId)
            => InvalidateChatroomListCacheAsync(new[] { userId });

        private Task InvalidateChatroomListCacheAsync(IEnumerable<Guid> userIds)
        {
            var tasks = userIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .SelectMany(userId => new[]
                {
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, false, null)),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, true, null)),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, false, "direct")),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, false, "group")),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, true, "direct")),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, true, "group")),
                });
            return Task.WhenAll(tasks);
        }

        //Helper methods
        private static void ApplyDeliverySummary(
            MessageResponse response,
            IEnumerable<MessageDelivery> deliveries)
        {
            var summary = BuildDeliverySummary(response.MessageId, deliveries);
            response.RecipientCount = summary.RecipientCount;
            response.DeliveredCount = summary.DeliveredCount;
            response.ReadCount = summary.ReadCount;
            response.DeliveryStatus = summary.DeliveryStatus;
        }
        private static List<ReactionSummaryResponse> BuildReactionSummaries(
            IEnumerable<MessageReaction> reactions,
            Guid currentUserId)
        {
            return reactions
                .GroupBy(r => r.EmojiCode)
                .Select(g => new ReactionSummaryResponse
                {
                    EmojiCode = g.Key,
                    Count = g.Count(),
                    ReactedByMe = g.Any(r => r.UserId == currentUserId),
                    Users = g.Select(r => new ReactionUserResponse
                    {
                        UserId = r.UserId,
                        Username = r.User?.Username ?? string.Empty,
                        Avatar = r.User?.Avatar
                    }).ToList()
                })
                .OrderByDescending(r => r.Count)
                .ToList();
        }

        private async Task<Dictionary<Guid, List<ReactionSummaryResponse>>> LoadReactionsByMessageIdsAsync(
            Guid[] messageIds,
            Guid currentUserId)
        {
            if (messageIds.Length == 0)
                return new Dictionary<Guid, List<ReactionSummaryResponse>>();

            var all = await _unitOfWork.MessageReactionRepository.GetByMessageIdsAsync(messageIds);
            return all
                .GroupBy(r => r.MessageId)
                .ToDictionary(g => g.Key, g => BuildReactionSummaries(g, currentUserId));
        }

        private static MessageDeliverySummaryEvent BuildDeliverySummary(
            Guid messageId,
            IEnumerable<MessageDelivery> deliveries)
        {
            var items = deliveries.ToList();
            var recipientCount = items.Count;
            var deliveredCount = items.Count(item => item.Status is "delivered" or "read");
            var readCount = items.Count(item => item.Status == "read");

            return new MessageDeliverySummaryEvent
            {
                MessageId = messageId,
                RecipientCount = recipientCount,
                DeliveredCount = deliveredCount,
                ReadCount = readCount,
                DeliveryStatus = recipientCount > 0 && readCount == recipientCount
                    ? "read"
                    : recipientCount > 0 && deliveredCount == recipientCount
                        ? "delivered"
                        : "sent"
            };
        }
        
        private async Task BroadcastMessageDeliveredAsync(MessageDeliveredEvent deliveredEvent)
        {
            try
            {
                await _hubContext.Clients.Group(deliveredEvent.ChatroomId.ToString())
                    .SendAsync("MessageDelivered", deliveredEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SignalR broadcast failed for MessageDelivered. MessageId={MessageId}, ChatroomId={ChatroomId}, DeliveredBy={DeliveredBy}",
                    deliveredEvent.Message.MessageId,
                    deliveredEvent.ChatroomId,
                    deliveredEvent.DeliveredBy);
            }
        }

        private async Task BroadcastMessageReadAsync(MessageReadEvent readEvent)
        {
            try
            {
                await _hubContext.Clients.Group(readEvent.ChatroomId.ToString())
                    .SendAsync("MessageRead", readEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SignalR broadcast failed for MessageRead. MessageId={MessageId}, ChatroomId={ChatroomId}, ReadBy={ReadBy}",
                    readEvent.Message.MessageId,
                    readEvent.ChatroomId,
                    readEvent.ReadBy);
            }
        }

        private async Task BroadcastAllMessagesReadAsync(
            Guid chatroomId,
            Guid userId,
            DateTime readAt,
            IReadOnlyCollection<Guid> messageIds)
        {
            var deliveries = messageIds.Count == 0
                ? new List<MessageDelivery>()
                : await _unitOfWork.MessageDeliveryRepository.GetByMessageIdsAsync(messageIds);
            var readEvent = new AllMessagesReadEvent
            {
                ChatroomId = chatroomId,
                ReadBy = userId,
                ReadAt = readAt,
                Messages = deliveries.GroupBy(item => item.MessageId)
                    .Select(group => BuildDeliverySummary(group.Key, group))
                    .ToList()
            };

            _logger.LogInformation(
                "Messages marked as read. ChatroomId={ChatroomId}, UserId={UserId}, Count={Count}, ReadAt={ReadAt}",
                chatroomId, userId, messageIds.Count, readAt);
            try
            {
                await _hubContext.Clients.Group(chatroomId.ToString())
                    .SendAsync("AllMessagesRead", readEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SignalR broadcast failed for AllMessagesRead. ChatroomId={ChatroomId}, ReadBy={ReadBy}, Count={Count}",
                    chatroomId, userId, messageIds.Count);
            }
        }

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

                var mentionedUserIds = await _unitOfWork.MessageMentions
                    .Query()
                    .Where(m => m.MessageId == message.MessageId)
                    .Select(m => m.MentionedUserId)
                    .ToListAsync();

                var members = await _unitOfWork.ChatroomMemberRepository
                    .GetActiveMembersWithUserAsync(message.ChatroomId);

                var mentionedSet = mentionedUserIds.ToHashSet();
                var recipientIds = new List<Guid>();

                foreach (var member in members.Where(m => m.UserId != senderId))
                {
                    var preference = (member.NotificationPreference ?? "all").Trim().ToLowerInvariant();
                    if (preference == "mute")
                        continue;
                    if (preference == "mentions" && !mentionedSet.Contains(member.UserId))
                        continue;

                    recipientIds.Add(member.UserId);
                }

                await _messageNotificationService.NotifyNewMessageAsync(
                    message,
                    sender,
                    chatroom,
                    recipientIds,
                    mentionedUserIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message notifications");
                throw;
            }
        }

        private const int MaxPinnedMessagesPerChatroom = 20;

        public async Task<PinnedMessageResponse> PinMessageAsync(Guid userId, Guid messageId)
        {
            var message = await _unitOfWork.Messages.Query()
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.MessageId == messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            if (message.IsDeleted == true)
                throw new InvalidOperationException("Không thể ghim tin nhắn đã xóa.");

            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(message.ChatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");

            await EnsureCanPinAsync(userId, chatroom);

            var alreadyPinned = await _unitOfWork.PinnedMessages.AnyAsync(p =>
                p.ChatroomId == message.ChatroomId && p.MessageId == messageId);
            if (alreadyPinned)
                throw new InvalidOperationException("Tin nhắn đã được ghim.");

            var pinnedCount = await _unitOfWork.PinnedMessages.Query()
                .CountAsync(p => p.ChatroomId == message.ChatroomId);
            if (pinnedCount >= MaxPinnedMessagesPerChatroom)
                throw new InvalidOperationException(
                    $"Mỗi phòng chat chỉ được ghim tối đa {MaxPinnedMessagesPerChatroom} tin nhắn.");

            var pinnedBy = await _unitOfWork.Users.GetByIdAsync(userId);
            var pin = new PinnedMessage
            {
                PinnedMessageId = Guid.NewGuid(),
                ChatroomId = message.ChatroomId,
                MessageId = message.MessageId,
                PinnedByUserId = userId,
                PinnedAt = DateTime.UtcNow
            };

            await _unitOfWork.PinnedMessages.AddAsync(pin);
            await _unitOfWork.SaveChangesAsync();

            var response = ToPinnedResponse(pin, message, pinnedBy);

            try
            {
                await _hubContext.Clients.Group(message.ChatroomId.ToString())
                    .SendAsync("MessagePinned", new MessagePinnedEvent
                    {
                        ChatroomId = message.ChatroomId,
                        PinnedMessage = response
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SignalR broadcast failed for MessagePinned. MessageId={MessageId}", messageId);
            }

            return response;
        }

        public async Task UnpinMessageAsync(Guid userId, Guid messageId)
        {
            var pin = await _unitOfWork.PinnedMessages.Query()
                .FirstOrDefaultAsync(p => p.MessageId == messageId)
                ?? throw new KeyNotFoundException("Tin nhắn chưa được ghim.");

            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(pin.ChatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");

            await EnsureCanPinAsync(userId, chatroom);

            _unitOfWork.PinnedMessages.Remove(pin);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.Group(pin.ChatroomId.ToString())
                    .SendAsync("MessageUnpinned", new MessageUnpinnedEvent
                    {
                        ChatroomId = pin.ChatroomId,
                        MessageId = messageId,
                        UnpinnedBy = userId
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SignalR broadcast failed for MessageUnpinned. MessageId={MessageId}", messageId);
            }
        }

        public async Task<List<PinnedMessageResponse>> GetPinnedMessagesAsync(Guid userId, Guid chatroomId)
        {
            var isMember = await _unitOfWork.ChatroomMemberRepository
                .HasActiveMemberAsync(chatroomId, userId);
            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của phòng chat này.");

            var pins = await _unitOfWork.PinnedMessages.Query()
                .Where(p => p.ChatroomId == chatroomId)
                .Include(p => p.Message).ThenInclude(m => m.Sender)
                .Include(p => p.PinnedByUser)
                .OrderByDescending(p => p.PinnedAt)
                .ToListAsync();

            return pins
                .Where(p => p.Message.IsDeleted != true)
                .Select(p => ToPinnedResponse(p, p.Message, p.PinnedByUser))
                .ToList();
        }

        private async Task EnsureCanPinAsync(Guid userId, Chatroom chatroom)
        {
            var member = await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberAsync(chatroom.ChatroomId, userId);
            if (member is null)
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của phòng chat này.");

            if (string.Equals(chatroom.RoomType, "direct", StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(member.MemberRole, "admin", StringComparison.OrdinalIgnoreCase))
                return;

            var canPin = await _unitOfWork.MemberPermissionRepository
                .HasPermissionAsync(userId, chatroom.ChatroomId, PermissionType.CanPinMessages);
            if (!canPin)
                throw new UnauthorizedAccessException("Bạn không có quyền ghim tin nhắn trong nhóm này.");
        }

        private static PinnedMessageResponse ToPinnedResponse(
            PinnedMessage pin,
            Message message,
            User? pinnedBy)
        {
            return new PinnedMessageResponse
            {
                PinnedMessageId = pin.PinnedMessageId,
                ChatroomId = pin.ChatroomId,
                MessageId = pin.MessageId,
                MessageType = message.MessageType,
                MessageText = message.IsDeleted == true
                    ? "Tin nhắn đã bị xóa"
                    : message.MessageText ?? string.Empty,
                SenderId = message.SenderId ?? Guid.Empty,
                SenderFullname = message.Sender?.Fullname
                    ?? message.Sender?.Username
                    ?? string.Empty,
                PinnedByUserId = pin.PinnedByUserId,
                PinnedByName = pinnedBy?.Fullname ?? pinnedBy?.Username ?? string.Empty,
                PinnedAt = pin.PinnedAt
            };
        }

    }
}
