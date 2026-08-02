using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class MessagePollVote
    {
        public Guid VoteId { get; set; }
        public Guid OptionId { get; set; }
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public DateTime VotedAt { get; set; }

        public virtual MessagePollOption Option { get; set; } = null!;
        public virtual MessagePoll Poll { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
