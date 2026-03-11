using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Messages
{
    public class ReplyPreviewRequest
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}