namespace linksy_backend_api.Domain.DTOs.Responses.Reactions
{
    public class ReactionUserResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }
}
