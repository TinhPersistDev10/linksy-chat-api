namespace linksy_backend_api.DTOs.MessagesDTOs
{
    public class MentionDto
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
