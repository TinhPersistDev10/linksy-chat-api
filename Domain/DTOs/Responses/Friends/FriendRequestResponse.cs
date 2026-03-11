using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Responses.Friends
{
    public class FriendRequestResponse
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = "pending";
        public string? Message { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        // Sender info
        public Guid SenderId { get; set; }
        public string? SenderUsername { get; set; }
        public string? SenderAvatar { get; set; }
        public string? SenderFullname { get; set; }

        // Receiver info
        public Guid ReceiverId { get; set; }
        public string? ReceiverUsername { get; set; }
        public string? ReceiverFullname { get; set; }
        public string? ReceiverAvatar { get; set; }
    }
}