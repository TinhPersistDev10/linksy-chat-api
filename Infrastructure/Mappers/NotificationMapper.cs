using linksy_backend_api.Core.DTOs.Responses.Notifications;
using linksy_backend_api.Models;

namespace linksy_backend_api.Infrastructure.Mappers
{
    public static class NotificationMapper
    {
        public static NotificationResponse ToResponse(Notification notification)
        {
            return new NotificationResponse
            {
                NotificationId    = notification.NotificationId,
                NotificationType  = notification.NotificationType,
                Title             = notification.Title ?? string.Empty,
                Body              = notification.Body ?? string.Empty,
                RelatedEntityId   = notification.RelatedEntityId,
                RelatedEntityType = notification.RelatedEntityType,
                ActionUrl         = notification.ActionUrl,
                ImageUrl          = notification.ImageUrl,
                IsRead            = notification.IsRead ?? false,
                ReadAt            = notification.ReadAt,
                CreatedAt         = notification.CreatedAt ?? DateTime.UtcNow
            };
        }

        public static List<NotificationResponse> ToResponseList(IEnumerable<Notification> notifications)
            => notifications.Select(ToResponse).ToList();
    }
}