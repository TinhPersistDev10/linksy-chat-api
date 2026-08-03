using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Core.DTOs.Responses.Notifications;
using linksy_backend_api.DTOs;
using linksy_backend_api.Models;

namespace linksy_backend_api.Core.Interfaces.Services
{
    public interface INotificationService
    {
        Task<Notification?> CreateNotificationAsync(CreateNotificationRequest notification);
        Task CreateBulkNotificationsAsync(List<CreateNotificationRequest> requestsList);

        Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<List<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<ApiResponseDto> MarkAsReadAsync(Guid userId, Guid notificationId);
        Task<ApiResponseDto> MarkAllAsReadAsync(Guid userId);
        
        Task<ApiResponseDto> DeleteNotificationAsync(Guid userId, Guid notificationId);
        Task<ApiResponseDto> DeleteAllNotificationsAsync(Guid userId);

        Task NotifyNewMessageAsync(
            Message message,
            User sender,
            Chatroom chatroom,
            List<Guid> recipientIds,
            List<Guid>? mentionedUserIds = null);
        Task NotifyNewFriendRequestAsync(Guid senderId, Guid receiverId, Guid requestId);
        Task NotifyFriendRequestAcceptedAsync(Guid accepterId, Guid senderId, Guid friendshipId);
        Task NotifyGroupInvitationAsync(Guid invitedBy, Guid invitedUserId, Guid chatroomId, Guid invitationId);
    }
}