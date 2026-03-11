using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Domain.DTOs.Responses.Chatrooms
{
    public class MemberInfoResponseDto
    {
        public Guid MemberId { get; set; }
        public string MemberRole { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public bool IsMuted { get; set; }
        public DateTime? MutedUntil { get; set; }
        public string NotificationPreference { get; set; } = "all";
        public int MessageCount { get; set; }
        public DateTime? LastReadAt { get; set; }
        public DateTime JoinedAt { get; set; }
        public MemberPermissionResponseDto Permissions { get; set; } = new();
    }
}               