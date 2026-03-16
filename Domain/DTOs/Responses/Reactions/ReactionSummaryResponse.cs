namespace linksy_backend_api.Domain.DTOs.Responses.Reactions
{
    public class ReactionSummaryResponse
    {
        /// <summary>Unicode emoji, ví dụ "👍", "❤️", "😂"</summary>
        public string EmojiCode { get; set; } = string.Empty;
        public int Count { get; set; }
        /// <summary>True nếu user đang gọi đã react với emoji này.</summary>
        public bool ReactedByMe { get; set; }
        public List<ReactionUserResponse> Users { get; set; } = new();
    }
}
