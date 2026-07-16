using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class CallLog
    {
        public Guid Id { get; set; }

        // Ai gọi
        public Guid CallerId { get; set; }
        public User Caller { get; set; } = null!;

        // Gọi trong chatroom nào (1-1 hoặc group)
        public Guid ChatroomId { get; set; }
        public Chatroom Chatroom { get; set; } = null!;

        public string CallType { get; set; } = "video"; // "video" | "audio"
        public string Status { get; set; } = "ringing"; // "ringing" | "answered" | "missed" | "rejected" | "busy"

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AnsweredAt { get; set; }  // lúc có người bắt máy
        public DateTime? EndedAt { get; set; }
        public int? DurationSec { get; set; }      // tính từ AnsweredAt -> EndedAt

        // Navigation
        public ICollection<CallParticipant> Participants { get; set; } = new List<CallParticipant>();
    }
}