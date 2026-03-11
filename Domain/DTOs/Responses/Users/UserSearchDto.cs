using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.UserDTO
{
    public class UserSearchDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string? Fullname { get; set; }
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public string RelationshipStatus { get; set; }
        public string ActionButtonText { get; set; }
        public bool CanSendRequest { get; set; }
    }
}