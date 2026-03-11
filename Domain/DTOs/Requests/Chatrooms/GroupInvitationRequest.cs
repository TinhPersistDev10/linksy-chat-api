using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class GroupInvitationRequest
    {
        public Guid InvitationId { get; set; }
        public Guid ChatroomId { get; set; }
        public string ChatroomName { get; set; } = string.Empty;
        public string ChatroomAvatar { get; set; } = string.Empty;
        public Guid InvitedByUserId { get; set; }
        public string InvitedByUsername { get; set; } = string.Empty;
        public string InvitedByFullname { get; set; } = string.Empty;
        public string InvitedByAvatar { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}