using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class MessagePoll
    {
        public Guid MessageId { get; set; }
        public string Question { get; set; } = null!;
        public bool IsClosed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public Guid CreatedByUserId { get; set; }

        public virtual Message Message { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual ICollection<MessagePollOption> Options { get; set; } = new List<MessagePollOption>();
    }
}
