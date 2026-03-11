using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.Block
{
    public class BlockedUserResponse
    {
        public Guid BlockId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;    
        public string Fullname { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; }
    }
}