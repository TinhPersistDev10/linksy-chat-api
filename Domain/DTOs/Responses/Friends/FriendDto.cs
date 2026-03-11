using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.FriendDTO
{
    public class FriendDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime FriendsSince { get; set; }
    }
}