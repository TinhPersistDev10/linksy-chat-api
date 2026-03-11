using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.MessagesDTOs
{
    public class SendMessageRequest
    {
        [Required]
        public Guid ChatroomId { get; set; }

        [Required]
        public string MessageType { get; set; } = string.Empty; // "text", "image", "file", "video", "audio"

        public string MessageText { get; set; } = string.Empty;

        public Guid? ParentMessageId { get; set; } // For replies
    }
}