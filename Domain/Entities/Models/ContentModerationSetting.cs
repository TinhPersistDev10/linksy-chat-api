namespace linksy_backend_api.Domain.Entities.Models
{
    public class ContentModerationSetting
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public List<string> BannedWords { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
    }
}
