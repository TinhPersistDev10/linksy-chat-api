using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Core.DTOs.Requests.Friends
{
    public class FriendRequestDto
    {
        public Guid RequestId { get; set; }
        public Guid? SenderId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderFullname { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public Guid? ReceiverId { get; set; }
        public string ReceiverUsername { get; set; } = string.Empty;
        public string ReceiverFullname { get; set; } = string.Empty;
        public string ReceiverAvatar { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}