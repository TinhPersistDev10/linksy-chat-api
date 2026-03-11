using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class UpdateChatroomRequest
    {
        [StringLength(100)]
        public string RoomName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public string Avatar { get; set; } = string.Empty;
    }
}