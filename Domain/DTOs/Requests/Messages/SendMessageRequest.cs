using System.ComponentModel.DataAnnotations;
using linksy_backend_api.Domain.DTOs.Requests;

namespace linksy_backend_api.DTOs.MessagesDTOs
{
    public class SendMessageRequest
    {
        [Required]
        public Guid ChatroomId { get; set; }

        [Required]
        public string MessageType { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;

        public Guid? ParentMessageId { get; set; } // For replies
        public List<SendMessageAttachmentRequest>? Attachments { get; set; }

        /// <summary>User IDs mentioned via @ in group messages.</summary>
        public List<Guid>? Mentions { get; set; }
    }
}