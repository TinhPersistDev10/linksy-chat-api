using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Responses.Chatrooms
{
    public class GroupInvitationResponseDto
    {
        public Guid InvitationId { get; set; }
        public Guid ChatroomId { get; set; }
        public string ChatroomName { get; set; } = string.Empty;
        public string? ChatroomAvatar { get; set; }
        public int MemberCount { get; set; }
        
        public Guid InvitedBy { get; set; }
        public string InvitedByUsername { get; set; } = string.Empty;
        public string InvitedByFullname { get; set; } = string.Empty;
        public string? InvitedByAvatar { get; set; }
        
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        
        public DateTime SentAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}