using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Responses.Users
{
    public class UserUpdateResponse
    {
        public string? Username { get; set; }
        public string? Fullname { get; set; }
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }
}
