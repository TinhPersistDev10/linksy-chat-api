namespace linksy_backend_api.Domain.Entities.Models
{
    public class MessagePollOption
    {
        public Guid OptionId { get; set; }
        public Guid MessageId { get; set; }
        public string Text { get; set; } = null!;
        public int SortOrder { get; set; }

        public virtual MessagePoll Poll { get; set; } = null!;
        public virtual ICollection<MessagePollVote> Votes { get; set; } = new List<MessagePollVote>();
    }
}
