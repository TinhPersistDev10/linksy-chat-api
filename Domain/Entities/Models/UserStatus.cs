using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class UserStatus
    {
        public Guid StatusId { get; set; }
        public Guid UserId { get; set; }
        public string StatusType { get; set; } = null!;  // online | away | busy | offline
        public string? CustomStatus { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;
    }
}