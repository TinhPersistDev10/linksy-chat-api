using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.MessageDTO
{
    public class EditMessageRequest
    {
        [Required]
        [StringLength(5000)]
        public string MessageText { get; set; } = null!;
    }
}