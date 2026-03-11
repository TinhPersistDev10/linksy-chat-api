using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Users
{
    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Fullname { get; set; }
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? IsEmailVerified { get; set; }
    }
}