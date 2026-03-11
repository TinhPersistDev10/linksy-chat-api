using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Core.DTOs.Responses.Notifications;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.Hubs;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
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
        private readonly IHubContext<ChatHub> _hubContext;

        public NotificationService(IUnitOfWork unitOfWork, IConnectionManager connectionManager, ILogger<NotificationService> logger, IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _connectionManager = connectionManager;
            _logger = logger;
            _hubContext = hubContext;
        }
        #region Core Notification Management
        public async Task<Notification> CreateNotificationAsync(CreateNotificationRequest notification)
        {
            try
            {
                var notificationEntity = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = notification.UserId,
                    NotificationType = notification.NotificationType,
                    Title = notification.Title,
                    Body = notification.Body,
                    RelatedEntityId = notification.RelatedEntityId,
                    RelatedEntityType = notification.RelatedEntityType,
                    ActionUrl = notification.ActionUrl,
                    ImageUrl = notification.ImageUrl,
                    IsRead = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Notifications.AddAsync(notificationEntity);
                await _unitOfWork.SaveChangesAsync();

                await SendRealTimeNotificationAsync(notificationEntity);

                return notificationEntity;
            }
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "Error creating notification for user {UserId}", notification.UserId);
                throw;
            }
        }



        public async Task CreateBulkNotificationsAsync(List<CreateNotificationRequest> requestsList)
        {
            try
            {
                var notifications = new List<Notification>();

                foreach (var notify in requestsList)
                {
                    notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = notify.UserId,
                        NotificationType = notify.NotificationType,
                        Title = notify.Title,
                        Body = notify.Body,
                        RelatedEntityId = notify.RelatedEntityId,
                        RelatedEntityType = notify.RelatedEntityType,
                        ActionUrl = notify.ActionUrl,
                        ImageUrl = notify.ImageUrl,
                        IsRead = false,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _unitOfWork.Notifications.AddRangeAsync(notifications);
                await _unitOfWork.SaveChangesAsync();
                foreach (var notify in notifications)
                {
                    await SendRealTimeNotificationAsync(notify);
                }
                return;

            }
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "Error creating bulk notifications");
                throw;
            }
        }
        #endregion

        public async Task<List<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId)
        {
            try
            {
                var unreadNotifications = await _unitOfWork.NotificationRepository.GetUnreadNotificationsAsync(userId);
                var result = new List<NotificationResponse>();
                foreach (var noti in unreadNotifications)
                {
                    var dto = MapToNotificationResponseAsync(noti);
                    result.Add(dto);
                }

                return result;
            }
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "Error getting unread notifications for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var notifications = await _unitOfWork.NotificationRepository.GetUserNotificationsAsync(userId, page, pageSize);
                var result = new List<NotificationResponse>();
                foreach (var noti in notifications)
                {
                    var dto = MapToNotificationResponseAsync(noti);
                    result.Add(dto);
                }

                return result;
            }
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "Error getting user notifications for user {UserId}", userId);
                throw;
            }


        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                var unreadCount = await _unitOfWork.NotificationRepository.GetUnreadCountAsync(userId);
                return unreadCount;
            }
            catch (System.Exception)
            {
                _logger.LogError("Error getting unread count for user {UserId}", userId);
                throw;
            }
        }
        public async Task NotifyFriendRequestAcceptedAsync(Guid accepterId, Guid senderId, Guid friendshipId)
        {
            try
            {
                var accepter = await _unitOfWork.Users.GetByIdAsync(accepterId);
                if (accepter == null)
                {
                    _logger.LogWarning("Accepter not found with ID {AccepterId}", accepterId);
                    return;
                }
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
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating friend accepted notification");
                throw;
            }
        }


        public async Task NotifyNewFriendRequestAsync(Guid senderId, Guid receiverId, Guid requestId)
        {
            try
            {
                var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
                if (sender == null)
                {
                    _logger.LogWarning("Sender not found with ID {SenderId}", senderId);
                    return;
                }
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
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating friend request notification");
                throw;
            }
        }

        public async Task NotifyNewMessageAsync(Message message, User sender, Chatroom chatroom, List<Guid> recipientIds)
        {
            try
            {
                string nameChatroom = chatroom.RoomName ?? "Chat";
                if (chatroom.RoomType == "direct")
                {
                    nameChatroom = sender.Fullname ?? sender.Username ?? "Chat";
                }
                var notifications = recipientIds.Select(recipientId => new CreateNotificationRequest
                {
                    UserId = recipientId,
                    NotificationType = "NewMessage",
                    Title = $"New message in {nameChatroom}",
                    Body = message.MessageText ?? string.Empty,
                    RelatedEntityId = message.MessageId,
                    RelatedEntityType = "Message",
                    ActionUrl = $"/messages/{chatroom.ChatroomId}",
                    ImageUrl = sender.Avatar
                }).ToList();

                await CreateBulkNotificationsAsync(notifications);

            }
            catch (System.Exception)
            {

                throw;
            }
        }
        public Task<ApiResponseDto> DeleteAllNotificationsAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponseDto> DeleteNotificationAsync(Guid userId, Guid notificationId)
        {
            throw new NotImplementedException();
        }

        #region RealTimeNotification
        private async Task SendRealTimeNotificationAsync(Notification notification)
        {
            try
            {
                var connections = await _connectionManager.GetConnectionsAsync(notification.UserId);
                if (connections.Any())
                {
                    var notificationResponse = MapToNotificationResponseAsync(notification);
                    await _hubContext.Clients.Clients(connections).SendAsync("ReceiveNotification", notificationResponse);
                }
            }
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "Error sending realtime notification");
            }
        }
        #endregion

        #region Mapping
        private NotificationResponse MapToNotificationResponseAsync(Notification notification)
        {
            return new NotificationResponse
            {
                NotificationId = notification.NotificationId,
                NotificationType = notification.NotificationType,
                Title = notification.Title ?? string.Empty,
                Body = notification.Body ?? string.Empty,
                RelatedEntityId = notification.RelatedEntityId,
                RelatedEntityType = notification.RelatedEntityType,
                ActionUrl = notification.ActionUrl,
                ImageUrl = notification.ImageUrl,
                IsRead = notification.IsRead ?? false,
                ReadAt = notification.ReadAt,
                CreatedAt = notification.CreatedAt ?? DateTime.UtcNow
            };
        }

        #endregion
        public Task<ApiResponseDto> MarkAllAsReadAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponseDto> MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            throw new NotImplementedException();
        }

        public Task NotifyGroupInvitationAsync(Guid invitedBy, Guid invitedUserId, Guid chatroomId, Guid invitationId)
        {
            throw new NotImplementedException();
        }

    }
}