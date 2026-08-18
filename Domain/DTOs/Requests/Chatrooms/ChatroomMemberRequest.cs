using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class ChatroomMemberRequest
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string MemberRole { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastReadAt { get; set; }
        public Guid? AddedBy { get; set; }
        public string? AddedByUsername { get; set; }
        public string? AddedByFullname { get; set; }
    }
}