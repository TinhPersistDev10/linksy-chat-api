using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.AdminDTOs
{
    public class UpdateUserByAdminDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? Bio { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsEmailVerified { get; set; }
    }
}