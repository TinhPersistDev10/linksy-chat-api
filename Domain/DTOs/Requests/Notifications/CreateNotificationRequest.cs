using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Notifications
{
    public class CreateNotificationRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Body { get; set; } = string.Empty;

        public Guid? RelatedEntityId { get; set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; set; }

        [StringLength(500)]
        public string? ActionUrl { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

    }
}