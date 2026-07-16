namespace linksy_backend_api.Domain.DTOs.Responses.Reactions
{
    public class MessageReactionsResponse
    {
        public Guid MessageId { get; set; }
        /// <summary>Danh sách reaction đã gom theo emoji, sort theo Count giảm dần.</summary>
        public List<ReactionSummaryResponse> Reactions { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
