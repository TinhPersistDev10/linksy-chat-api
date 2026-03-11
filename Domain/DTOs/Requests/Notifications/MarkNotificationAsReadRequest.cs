using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Notifications
{
    public class MarkNotificationAsReadRequest
    {
        [Required]
        public Guid NotificationId { get; set; }
    }
}