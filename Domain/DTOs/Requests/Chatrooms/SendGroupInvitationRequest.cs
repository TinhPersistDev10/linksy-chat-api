using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class SendGroupInvitationRequest
    {
        [Required]
        public Guid InvitedUserId { get; set; }

        [StringLength(500)]
        public string Message { get; set; }  = string.Empty;
    }
}