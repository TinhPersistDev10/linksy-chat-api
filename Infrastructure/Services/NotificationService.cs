using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Core.DTOs.Responses.Notifications;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.Hubs;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Infrastructure.Mappers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.SignalR;

namespace linksy_backend_api.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<NotificationService> _logger;
        private readonly ICacheService _cache;
        private readonly IHubContext<ChatHub> _hubContext;
        private record CountWrapper { public int Count { get; init; } }
        public NotificationService(
            IUnitOfWork unitOfWork,
            IConnectionManager connectionManager,
            ILogger<NotificationService> logger,
            ICacheService cache,
            IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _connectionManager = connectionManager;
            _hubContext = hubContext;
            _cache = cache;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CORE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<Notification> CreateNotificationAsync(CreateNotificationRequest request)
        {
            try
            {
                var entity = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = request.UserId,
                    NotificationType = request.NotificationType,
                    Title = request.Title,
                    Body = request.Body,
                    RelatedEntityId = request.RelatedEntityId,
                    RelatedEntityType = request.RelatedEntityType,
                    ActionUrl = request.ActionUrl,
                    ImageUrl = request.ImageUrl,
                    IsRead = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Notifications.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();
                await _cache.RemoveAsync(CacheKeys.NotifUnreadCount(request.UserId));
                await SendRealTimeAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task CreateBulkNotificationsAsync(List<CreateNotificationRequest> requests)
        {
            try
            {
                var entities = requests.Select(r => new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = r.UserId,
                    NotificationType = r.NotificationType,
                    Title = r.Title,
                    Body = r.Body,
                    RelatedEntityId = r.RelatedEntityId,
                    RelatedEntityType = r.RelatedEntityType,
                    ActionUrl = r.ActionUrl,
                    ImageUrl = r.ImageUrl,
                    IsRead = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _unitOfWork.Notifications.AddRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();
                var affectedUserIds = entities.Select(entity => entity.UserId).Distinct();
                await Task.WhenAll(affectedUserIds.Select(userId =>
                    _cache.RemoveAsync(CacheKeys.NotifUnreadCount(userId))));
                var realtimeTasks = entities.Select(entity => SendRealTimeAsync(entity));
                await Task.WhenAll(realtimeTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk notifications");
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var notifications = await _unitOfWork.NotificationRepository.GetUserNotificationsAsync(userId, page, pageSize);
                return NotificationMapper.ToResponseList(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId)
        {
            try
            {
                var notifications = await _unitOfWork.NotificationRepository.GetUnreadNotificationsAsync(userId);
                return NotificationMapper.ToResponseList(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notifications for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            var cacheKey = CacheKeys.NotifUnreadCount(userId);

            var wrapper = await _cache.GetOrSetAsync(
                cacheKey,
                async () =>
                    new CountWrapper
                    {
                        Count = await _unitOfWork.NotificationRepository.GetUnreadCountAsync(userId)
                    },
                CacheKeys.ShortTtl);
            return wrapper.Count;

        }


        // ─────────────────────────────────────────────────────────────────────
        // MARK AS READ
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await _unitOfWork.Notifications.FirstOrDefaultAsync(
                n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
                return new ApiResponseDto { Success = false, Message = "Không tìm thấy thông báo." };

            if (notification.IsRead == true)
                return new ApiResponseDto { Success = true, Message = "Thông báo đã được đọc." };

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKeys.NotifUnreadCount(userId));
            return new ApiResponseDto { Success = true, Message = "Đã đánh dấu đã đọc." };
        }

        public async Task<ApiResponseDto> MarkAllAsReadAsync(Guid userId)
        {
            await _unitOfWork.NotificationRepository.MarkAllAsReadAsync(userId);
            await _cache.RemoveAsync(CacheKeys.NotifUnreadCount(userId));
            return new ApiResponseDto { Success = true, Message = "Đã đánh dấu tất cả đã đọc." };
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> DeleteNotificationAsync(Guid userId, Guid notificationId)
        {
            var notification = await _unitOfWork.Notifications.FirstOrDefaultAsync(
                n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
                return new ApiResponseDto { Success = false, Message = "Không tìm thấy thông báo." };

            notification.IsDeleted = true;
            notification.DeletedAt = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();
            if (notification.IsRead != true) await _cache.RemoveAsync(CacheKeys.NotifUnreadCount(userId));
            return new ApiResponseDto { Success = true, Message = "Đã xóa thông báo." };
        }

        public async Task<ApiResponseDto> DeleteAllNotificationsAsync(Guid userId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.NotificationRepository.SoftDeleteAllSync(userId);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (System.Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting all notifications for UserId={UserId}", userId);
                throw;
            }
            await _cache.RemoveAsync(CacheKeys.NotifUnreadCount(userId));

            return new ApiResponseDto { Success = true, Message = "Đã xóa tất cả thông báo." };
        }

        // ─────────────────────────────────────────────────────────────────────
        // DOMAIN NOTIFICATIONS
        // ─────────────────────────────────────────────────────────────────────

        public async Task NotifyNewMessageAsync(
            Message message,
            User sender,
            Chatroom chatroom,
            List<Guid> recipientIds,
            List<Guid>? mentionedUserIds = null)
        {
            try
            {
                var roomName = chatroom.RoomType == "direct"
                    ? sender.Fullname ?? sender.Username ?? "Chat"
                    : chatroom.RoomName ?? "Chat";

                var mentionedSet = mentionedUserIds?.ToHashSet() ?? [];
                var previewBody = message.MessageType switch
                {
                    "image" => "Đã gửi một ảnh",
                    "file" => "Đã gửi một file",
                    "voice" or "audio" => "Đã gửi tin nhắn thoại",
                    _ => message.MessageText ?? string.Empty
                };

                foreach (var recipientId in recipientIds)
                {
                    var isMention = mentionedSet.Contains(recipientId);
                    var notificationType = isMention ? "mention" : "new_message";

                    if (isMention)
                    {
                        try
                        {
                            await CreateNotificationAsync(new CreateNotificationRequest
                            {
                                UserId = recipientId,
                                NotificationType = "mention",
                                Title = roomName,
                                Body = $"{sender.Fullname ?? sender.Username} đã nhắc đến bạn",
                                RelatedEntityId = message.MessageId,
                                RelatedEntityType = "message",
                                ActionUrl = $"/chat/{chatroom.ChatroomId}?messageId={message.MessageId}",
                                ImageUrl = sender.Avatar
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to persist mention notification. MessageId={MessageId}, UserId={UserId}",
                                message.MessageId,
                                recipientId);
                        }
                    }

                    var connections = await _connectionManager.GetConnectionsAsync(recipientId);
                    if (!connections.Any()) continue;

                    await _hubContext.Clients.Clients(connections)
                        .SendAsync("ReceiveMessageNotification", new
                        {
                            NotificationType = notificationType,
                            ChatroomId = chatroom.ChatroomId,
                            MessageId = message.MessageId,
                            SenderId = sender.UserId,
                            SenderName = sender.Fullname ?? sender.Username,
                            Title = roomName,
                            Body = isMention
                                ? $"{sender.Fullname ?? sender.Username} đã nhắc đến bạn: {previewBody}"
                                : previewBody,
                            SentAt = message.SentAt
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending realtime message notification");
                throw;
            }
        }

        public async Task NotifyNewFriendRequestAsync(Guid senderId, Guid receiverId, Guid requestId)
        {
            try
            {
                var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
                if (sender == null) { _logger.LogWarning("Sender not found {SenderId}", senderId); return; }

                await CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = receiverId,
                    NotificationType = "friend_request",
                    Title = "Lời mời kết bạn mới",
                    Body = $"{sender.Fullname ?? sender.Username} đã gửi lời mời kết bạn",
                    RelatedEntityId = requestId,
                    RelatedEntityType = "friend_request",
                    ActionUrl = "/friends/requests",
                    ImageUrl = sender.Avatar
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying friend request");
                throw;
            }
        }

        public async Task NotifyFriendRequestAcceptedAsync(Guid accepterId, Guid senderId, Guid friendshipId)
        {
            try
            {
                var accepter = await _unitOfWork.Users.GetByIdAsync(accepterId);
                if (accepter == null) { _logger.LogWarning("Accepter not found {AccepterId}", accepterId); return; }

                await CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = senderId,
                    NotificationType = "friend_accepted",
                    Title = "Yêu cầu kết bạn được chấp nhận",
                    Body = $"{accepter.Fullname ?? accepter.Username} đã chấp nhận lời mời kết bạn",
                    RelatedEntityId = friendshipId,
                    RelatedEntityType = "friendship",
                    ActionUrl = $"/profile/{accepterId}",
                    ImageUrl = accepter.Avatar
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying friend accepted");
                throw;
            }
        }

        public async Task NotifyGroupInvitationAsync(Guid invitedBy, Guid invitedUserId, Guid chatroomId, Guid invitationId)
        {
            try
            {
                var inviter = await _unitOfWork.Users.GetByIdAsync(invitedBy);
                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
                if (inviter == null || chatroom == null) return;

                await CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = invitedUserId,
                    NotificationType = "group_invitation",
                    Title = "Lời mời vào nhóm",
                    Body = $"{inviter.Fullname ?? inviter.Username} đã mời bạn vào nhóm {chatroom.RoomName}",
                    RelatedEntityId = invitationId,
                    RelatedEntityType = "group_invitation",
                    ActionUrl = $"/invitations/{invitationId}",
                    ImageUrl = chatroom.Avatar
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying group invitation");
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REALTIME
        // ─────────────────────────────────────────────────────────────────────

        private async Task SendRealTimeAsync(Notification notification)
        {
            try
            {
                var connections = await _connectionManager.GetConnectionsAsync(notification.UserId);
                if (connections.Any())
                    await _hubContext.Clients.Clients(connections)
                        .SendAsync("ReceiveNotification", NotificationMapper.ToResponse(notification));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending realtime notification");
            }
        }
    }
}
