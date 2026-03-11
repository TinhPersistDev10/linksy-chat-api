using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.AdminDTOs
{
    public class AssignRoleDto
    {
         public Guid UserId { get; set; }
        public int RoleId { get; set; }
    }
}