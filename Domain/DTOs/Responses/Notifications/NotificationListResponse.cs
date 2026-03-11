using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Responses.Notifications
{
    public class NotificationListResponse
    {
         public List<NotificationResponse> Notifications { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}