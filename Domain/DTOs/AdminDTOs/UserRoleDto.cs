using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.AdminDTOs
{
    public class UserRoleDto
    {
        public int UserRoleId { get; set; }
        public Guid UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}