namespace linksy_backend_api.Domain.DTOs.Responses.Settings
{
    public class PrivacySettingsResponse
    {
        public Guid Id { get; set; }
        public bool ReadReceiptsEnabled { get; set; }
        public bool TypingIndicatorsEnabled { get; set; }
        public bool LastSeenEnabled { get; set; }
        public string ProfilePhotoVisibility { get; set; } = "everyone";
        public string StatusVisibility { get; set; } = "everyone";
        public string WhoCanAddToGroups { get; set; } = "everyone";
        public string WhoCanMessageMe { get; set; } = "everyone";
    }
}
