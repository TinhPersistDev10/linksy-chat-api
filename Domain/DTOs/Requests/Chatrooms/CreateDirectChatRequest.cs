using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class CreateDirectChatRequest
    {
        [Required]
        public Guid OtherUserId { get; set; }
    }
}