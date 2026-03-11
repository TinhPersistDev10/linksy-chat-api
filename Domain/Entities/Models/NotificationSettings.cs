using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class NotificationSettings
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool NotificationsEnabled { get; set; }
        public bool NotificationSoundEnabled { get; set; }
        public bool MessagePreviewEnabled { get; set; }
        public bool EmailNotifications { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;
    }
}