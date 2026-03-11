using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class RemoveMemberRequest
    {
        [Required]
        public Guid ChatroomId { get; set; }

        [Required]
        public Guid MemberId { get; set; } = Guid.Empty;
    }
}