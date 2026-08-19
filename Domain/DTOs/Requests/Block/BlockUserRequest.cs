using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.Block
{
    public class BlockUserRequest
    {
        [Required]
        public Guid BlockedUserId { get; set; }

        [StringLength(2000)]
        public string Reason { get; set; } = string.Empty;
    } 
}