using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.FriendDTO
{
    public class SendFriendRequest
    {
        [Required]
        public Guid ReceiverId { get; set; }

        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}