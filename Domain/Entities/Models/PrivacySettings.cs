using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class PrivacySettings
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool ReadReceiptsEnabled { get; set; }
        public bool TypingIndicatorsEnabled { get; set; }
        public bool LastSeenEnabled { get; set; }
        public string ProfilePhotoVisibility { get; set; } = null!;  // everyone | friends | nobody
        public string StatusVisibility { get; set; } = null!;
        public string WhoCanAddToGroups { get; set; } = null!;
        /// <summary>everyone | friends — also applies to 1-1 calls.</summary>
        public string WhoCanMessageMe { get; set; } = null!;

        // Navigation
        public virtual User User { get; set; } = null!;
    }
}