using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Notifications
{
    public class DeleteNotificationsRequest
    {
        /// <summary>
        /// Specific notification IDs to delete
        /// </summary>
        public List<Guid>? NotificationIds { get; set; }

        /// <summary>
        /// Delete all notifications of specific type
        /// </summary>
        public string? NotificationType { get; set; }

        /// <summary>
        /// Delete all read notifications
        /// </summary>
        public bool? DeleteAllRead { get; set; }

        /// <summary>
        /// Delete notifications older than X days
        /// </summary>
        public int? DeleteOlderThanDays { get; set; }
    }
}