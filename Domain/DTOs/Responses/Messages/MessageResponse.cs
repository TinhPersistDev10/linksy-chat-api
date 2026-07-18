using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.DTOs.Responses.Messages;
using linksy_backend_api.Domain.DTOs.Responses.Reactions;

namespace linksy_backend_api.DTOs.MessagesDTOs
{
    public class MessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid ChatroomId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderFullname { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public string? SenderNickname { get; set; }

        public string MessageType { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;

        public Guid? ParentMessageId { get; set; }
        public MessageResponse? ParentMessage { get; set; }

        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsOwn { get; set; }

        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DeliveryStatus { get; set; } = "sent";
        public int RecipientCount { get; set; }
        public int DeliveredCount { get; set; }
        public int ReadCount { get; set; }
        public List<MediaAttachmentResponseDto>? Attachments { get; set; }
        public List<MentionDto>? Mentions { get; set; }
        public List<ReactionSummaryResponse> Reactions { get; set; } = new();
    }
}