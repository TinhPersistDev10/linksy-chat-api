using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Notifications
{
    public class GetNotificationsRequest
    {
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Filter by notification type
        /// </summary>
        public string? NotificationType { get; set; }

        /// <summary>
        /// Only get unread notifications
        /// </summary>
        public bool? OnlyUnread { get; set; }

        /// <summary>
        /// Filter by date range
        /// </summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by related entity
        /// </summary>
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }

        [Range(0, 2)]
        public int SortOrder { get; set; } = 0; // 0: NewestFirst, 1: OldestFirst, 2: UnreadFirst
    }
}