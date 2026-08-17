using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Core.DTOs.Responses.Notifications;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.Hubs;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Infrastructure.Mappers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<NotificationService> _logger;
        private readonly ICacheService _cache;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IEmailService _emailService;

        private record CountWrapper { public int Count { get; init; } }

        private record NotifSettingsCache
        {
            public bool NotificationsEnabled { get; init; }
            public bool NotificationSoundEnabled { get; init; }
            public bool MessagePreviewEnabled { get; init; }
            public bool EmailNotifications { get; init; }
        }

        public NotificationService(
            IUnitOfWork unitOfWork,
            IConnectionManager connectionManager,
            ILogger<NotificationService> logger,
            ICacheService cache,
            IHubContext<ChatHub> hubContext,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _connectionManager = connectionManager;
            _hubContext = hubContext;
            _cache = cache;
            _logger = logger;
            _emailService = emailService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CORE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<Notification?> CreateNotificationAsync(CreateNotificationRequest request)
        {
            try
            {
                var settings = await GetSettingsAsync(request.UserId);
                if (!settings.NotificationsEnabled)
                {
                    _logger.LogDebug(
                        "Skipped notification for user {UserId} (notifications disabled)",
                        request.UserId);
                    return null;
                }

                var body = request.Body;
                if (!settings.MessagePreviewEnabled && IsMessageRelatedType(request.NotificationType))
                {
                    body = request.NotificationType == "mention"
                        ? "Bạn được nhắc đến trong một cuộc trò chuyện"
                        : "Bạn có thông báo mới";
                }

                var entity = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = request.UserId,
                    NotificationType = request.NotificationType,
                    Title = request.Title,
                    Body = body,
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

                if (settings.EmailNotifications)
                    await TrySendNotificationEmailAsync(entity);

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
                if (requests.Count == 0) return;

                var settingsMap = await GetSettingsMapAsync(requests.Select(r => r.UserId));
                var entities = new List<Notification>();

                foreach (var r in requests)
                {
                    if (!settingsMap.TryGetValue(r.UserId, out var settings) || !settings.NotificationsEnabled)
                        continue;

                    var body = r.Body;
                    if (!settings.MessagePreviewEnabled && IsMessageRelatedType(r.NotificationType))
                    {
                        body = r.NotificationType == "mention"
                            ? "Bạn được nhắc đến trong một cuộc trò chuyện"
                            : "Bạn có thông báo mới";
                    }

                    entities.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = r.UserId,
                        NotificationType = r.NotificationType,
                        Title = r.Title,
                        Body = body,
                        RelatedEntityId = r.RelatedEntityId,
                        RelatedEntityType = r.RelatedEntityType,
                        ActionUrl = r.ActionUrl,
                        ImageUrl = r.ImageUrl,
                        IsRead = false,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (entities.Count == 0) return;

                await _unitOfWork.Notifications.AddRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();

                var affectedUserIds = entities.Select(entity => entity.UserId).Distinct().ToList();
                await Task.WhenAll(affectedUserIds.Select(userId =>
                    _cache.RemoveAsync(CacheKeys.NotifUnreadCount(userId))));

                await Task.WhenAll(entities.Select(entity => SendRealTimeAsync(entity)));

                var emailTasks = entities
                    .Where(e => settingsMap.TryGetValue(e.UserId, out var s) && s.EmailNotifications)
                    .Select(TrySendNotificationEmailAsync);
                await Task.WhenAll(emailTasks);
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
                    "sticker" => "Đã gửi một sticker",
                    "poll" => string.IsNullOrWhiteSpace(message.MessageText)
                        ? "Đã tạo một bình chọn"
                        : $"Bình chọn: {message.MessageText}",
                    _ => message.MessageText ?? string.Empty
                };

                var settingsMap = await GetSettingsMapAsync(recipientIds);

                foreach (var recipientId in recipientIds)
                {
                    if (!settingsMap.TryGetValue(recipientId, out var settings))
                        settings = DefaultSettings();

                    var isMention = mentionedSet.Contains(recipientId);
                    var notificationType = isMention ? "mention" : "new_message";
                    var alertsEnabled = settings.NotificationsEnabled;
                    var displayBody = !alertsEnabled || !settings.MessagePreviewEnabled
                        ? "Tin nhắn mới"
                        : previewBody;

                    // Persist mention only when global notifications are on
                    if (alertsEnabled && isMention)
                    {
                        try
                        {
                            await CreateNotificationAsync(new CreateNotificationRequest
                            {
                                UserId = recipientId,
                                NotificationType = "mention",
                                Title = roomName,
                                Body = settings.MessagePreviewEnabled
                                    ? $"{sender.Fullname ?? sender.Username} đã nhắc đến bạn"
                                    : "Bạn được nhắc đến trong một cuộc trò chuyện",
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

                    // Always tip the sidebar for unread; alerts/sound gated by flags
                    var connections = await _connectionManager.GetConnectionsAsync(recipientId);
                    if (!connections.Any()) continue;

                    string body;
                    if (!alertsEnabled || !settings.MessagePreviewEnabled)
                    {
                        body = isMention
                            ? "Bạn được nhắc đến trong một cuộc trò chuyện"
                            : "Tin nhắn mới";
                    }
                    else if (isMention)
                    {
                        body = $"{sender.Fullname ?? sender.Username} đã nhắc đến bạn: {previewBody}";
                    }
                    else
                    {
                        body = displayBody;
                    }

                    await _hubContext.Clients.Clients(connections)
                        .SendAsync("ReceiveMessageNotification", new
                        {
                            NotificationType = notificationType,
                            ChatroomId = chatroom.ChatroomId,
                            MessageId = message.MessageId,
                            SenderId = sender.UserId,
                            SenderName = sender.Fullname ?? sender.Username,
                            Title = roomName,
                            Body = body,
                            SentAt = message.SentAt,
                            AlertsEnabled = alertsEnabled,
                            NotificationSoundEnabled = alertsEnabled && settings.NotificationSoundEnabled,
                            MessagePreviewEnabled = alertsEnabled && settings.MessagePreviewEnabled
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

        public async Task NotifyFriendAvatarChangedAsync(Guid userId, string newAvatarUrl)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found when notifying avatar change {UserId}", userId);
                    return;
                }

                var friendIds = await _unitOfWork.FriendshipRepository.GetFriendIdsAsync(userId);
                if (friendIds.Count == 0) return;

                // Exclude friends with a block in either direction
                var blockedPairs = await _unitOfWork.BlockedUsers.Query()
                    .Where(b =>
                        (b.BlockerUserId == userId && friendIds.Contains(b.BlockedUserId)) ||
                        (b.BlockedUserId == userId && friendIds.Contains(b.BlockerUserId)))
                    .Select(b => b.BlockerUserId == userId ? b.BlockedUserId : b.BlockerUserId)
                    .ToListAsync();

                var blockedSet = blockedPairs.ToHashSet();
                var recipients = friendIds.Where(id => !blockedSet.Contains(id)).ToList();
                if (recipients.Count == 0) return;

                var displayName = user.Fullname ?? user.Username;
                var requests = recipients.Select(friendId => new CreateNotificationRequest
                {
                    UserId = friendId,
                    NotificationType = "friend_avatar_changed",
                    Title = "Bạn bè đổi ảnh đại diện",
                    Body = $"{displayName} vừa mới đổi ảnh đại diện",
                    RelatedEntityId = userId,
                    RelatedEntityType = "user",
                    ActionUrl = $"/dashboard",
                    ImageUrl = newAvatarUrl
                }).ToList();

                await CreateBulkNotificationsAsync(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying friends about avatar change for user {UserId}", userId);
                // Do not rethrow — avatar update already succeeded
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REALTIME / SETTINGS HELPERS
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

        private async Task<NotifSettingsCache> GetSettingsAsync(Guid userId)
        {
            var cacheKey = CacheKeys.UserNotifSettings(userId);
            var cached = await _cache.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var settings = await _unitOfWork.NotificationSettingsRepository.GetOrCreateAsync(userId);
                    await _unitOfWork.SaveChangesAsync();
                    return ToCache(settings);
                },
                CacheKeys.MediumTtl);

            return cached ?? DefaultSettings();
        }

        private async Task<Dictionary<Guid, NotifSettingsCache>> GetSettingsMapAsync(IEnumerable<Guid> userIds)
        {
            var ids = userIds.Distinct().ToList();
            var result = new Dictionary<Guid, NotifSettingsCache>();
            if (ids.Count == 0) return result;

            var missing = new List<Guid>();
            foreach (var id in ids)
            {
                var cached = await _cache.GetAsync<NotifSettingsCache>(CacheKeys.UserNotifSettings(id));
                if (cached is not null)
                    result[id] = cached;
                else
                    missing.Add(id);
            }

            if (missing.Count == 0) return result;

            var existing = await _unitOfWork.NotificationSettingsRepository.GetByUserIdsAsync(missing);
            var existingByUser = existing.ToDictionary(s => s.UserId);

            foreach (var userId in missing)
            {
                NotifSettingsCache cacheItem;
                if (existingByUser.TryGetValue(userId, out var settings))
                {
                    cacheItem = ToCache(settings);
                }
                else
                {
                    var created = await _unitOfWork.NotificationSettingsRepository.GetOrCreateAsync(userId);
                    cacheItem = ToCache(created);
                }

                result[userId] = cacheItem;
                await _cache.SetAsync(CacheKeys.UserNotifSettings(userId), cacheItem, CacheKeys.MediumTtl);
            }

            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        private static NotifSettingsCache ToCache(NotificationSettings settings) => new()
        {
            NotificationsEnabled = settings.NotificationsEnabled,
            NotificationSoundEnabled = settings.NotificationSoundEnabled,
            MessagePreviewEnabled = settings.MessagePreviewEnabled,
            EmailNotifications = settings.EmailNotifications
        };

        private static NotifSettingsCache DefaultSettings() => new()
        {
            NotificationsEnabled = true,
            NotificationSoundEnabled = true,
            MessagePreviewEnabled = true,
            EmailNotifications = false
        };

        private static bool IsMessageRelatedType(string? type) =>
            type is "mention" or "new_message";

        private async Task TrySendNotificationEmailAsync(Notification notification)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(notification.UserId);
                if (user is null || string.IsNullOrWhiteSpace(user.Email))
                    return;

                var displayName = user.Fullname ?? user.Username ?? "bạn";
                await _emailService.SendNotificationEmailAsync(
                    user.Email,
                    displayName,
                    notification.Title ?? "Thông báo",
                    notification.Body ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send notification email for NotificationId={NotificationId}",
                    notification.NotificationId);
            }
        }
    }
}
