using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class UserSettings
    {
        public Guid SettingId { get; set; }
        public Guid UserId { get; set; }
        public string Language { get; set; } = null!;
        public string Timezone { get; set; } = null!;
        public string Theme { get; set; } = null!;    // light | dark | system
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;
    }
}