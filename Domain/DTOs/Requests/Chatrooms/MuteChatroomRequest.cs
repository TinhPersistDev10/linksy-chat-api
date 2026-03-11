using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Chatrooms
{
    public class MuteChatroomRequest
    {
        [Required]
        public Guid ChatroomId { get; set; }

        [Required]
        public bool IsMuted { get; set; }

        public DateTime? MuteUntil { get; set; }
    }
}