namespace linksy_backend_api.Domain.DTOs.Responses.Settings
{
    public class UserSettingsResponse
    {
        public Guid SettingId { get; set; }
        public string Language { get; set; } = "vi";
        public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
        public string Theme { get; set; } = "system";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
