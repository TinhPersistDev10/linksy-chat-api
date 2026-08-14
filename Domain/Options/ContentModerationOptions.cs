namespace linksy_backend_api.Domain.Options
{
    public class ContentModerationOptions
    {
        public const string SectionName = "ContentModeration";

        public bool Enabled { get; set; } = true;

        public List<string> BannedWords { get; set; } = new();
    }
}
