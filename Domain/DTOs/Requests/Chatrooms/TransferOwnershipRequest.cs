using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Chatrooms
{
    public class TransferOwnershipRequest
    {
        [Required]
        public Guid ChatroomId { get; set; }

        [Required]
        public Guid NewOwnerId { get; set; }
    }
}