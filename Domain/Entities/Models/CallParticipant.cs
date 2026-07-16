using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class CallParticipant
    {
        public Guid Id { get; set; }

        public Guid CallLogId { get; set; }
        public CallLog CallLog { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Trạng thái từng người trong cuộc gọi
        public string Status { get; set; } = "invited"; // "invited" | "joined" | "declined" | "missed" | "left"
        public DateTime? JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
    }
}