namespace linksy_backend_api.Domain.DTOs.Responses.ContentModeration
{
    public class ContentModerationConfigDto
    {
        public bool Enabled { get; set; }
        public List<string> BannedWords { get; set; } = new();
    }
}
