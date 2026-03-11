using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Notifications
{
    public class MarkNotificationsAsReadRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 notification")]
        public List<Guid> NotificationIds { get; set; } = new List<Guid>();
    }
}