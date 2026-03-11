using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Responses.Chatrooms
{
    public class ChatroomMemberDetailResponseDto
    {
        public Guid MemberId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string MemberRole { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        
        public bool IsMuted { get; set; }
        public DateTime? MutedUntil { get; set; }
        public string NotificationPreference { get; set; } = "all";
        
        public int MessageCount { get; set; }
        public DateTime JoinedAt { get; set; }
        public Guid? AddedBy { get; set; }
        public string? AddedByUsername { get; set; }
        
        public MemberPermissionResponseDto Permissions { get; set; } = new();
    }
}