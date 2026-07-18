using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class PinnedMessage
    {
        public Guid PinnedMessageId { get; set; }
        public Guid ChatroomId { get; set; }
        public Guid MessageId { get; set; }
        public Guid PinnedByUserId { get; set; }
        public DateTime PinnedAt { get; set; }

        public virtual Chatroom Chatroom { get; set; } = null!;
        public virtual Message Message { get; set; } = null!;
        public virtual User PinnedByUser { get; set; } = null!;
    }
}
