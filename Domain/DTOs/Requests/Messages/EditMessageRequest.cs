using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.MessageDTO
{
    public class EditMessageRequest
    {
        [Required(ErrorMessage = "MessageText is required")]
        [StringLength(5000, ErrorMessage = "Message length: 5000 characters")]
        public string MessageText { get; set; } = null!;
    }
}