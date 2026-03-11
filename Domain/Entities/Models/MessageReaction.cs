using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class MessageReaction
    {
        public Guid ReactionId { get; set; }
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public string EmojiCode { get; set; } = null!;
        public DateTime ReactedAt { get; set; }

        // Navigation
        public virtual Message Message { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}