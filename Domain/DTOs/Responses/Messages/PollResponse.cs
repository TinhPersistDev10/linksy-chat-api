namespace linksy_backend_api.Domain.DTOs.Responses.Messages
{
    public class PollOptionResponse
    {
        public Guid OptionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public int VoteCount { get; set; }
        public bool VotedByMe { get; set; }
    }

    public class PollResponse
    {
        public Guid MessageId { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool IsClosed { get; set; }
        public Guid CreatedByUserId { get; set; }
        public int TotalVotes { get; set; }
        public Guid? MyVotedOptionId { get; set; }
        public bool CanClose { get; set; }
        public List<PollOptionResponse> Options { get; set; } = new();
    }

    public class PollUpdatedEvent
    {
        public Guid MessageId { get; set; }
        public Guid ChatroomId { get; set; }
        /// <summary>User who voted (null when poll is closed).</summary>
        public Guid? ActorUserId { get; set; }
        public Guid? ActorOptionId { get; set; }
        public PollResponse Poll { get; set; } = null!;
    }
}
