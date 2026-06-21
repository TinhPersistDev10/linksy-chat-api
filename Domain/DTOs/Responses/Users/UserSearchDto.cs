using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


//Tìm kiếm liên quan tới kết bạn
namespace linksy_backend_api.DTOs.UserDTO
{
    public class UserSearchDto
    {
        public Guid UserId { get; set; }
        public required string Username { get; set; } 
        public string? Fullname { get; set; }
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public required string RelationshipStatus { get; set; }
        public required string ActionButtonText { get; set; }
        public bool CanSendRequest { get; set; }
    }
}