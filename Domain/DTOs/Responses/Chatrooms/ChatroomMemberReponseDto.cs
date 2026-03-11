using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Responses.Chatrooms
{
    public class ChatroomMemberReponseDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string MemberRole { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public string? Nickname { get; set; }
    }
}