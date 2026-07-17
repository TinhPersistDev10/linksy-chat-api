using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class MessageMention
    {
        public Guid MessageId { get; set; }
        public Guid MentionedUserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Message Message { get; set; } = null!;
        public virtual User MentionedUser { get; set; } = null!;
    }
}
