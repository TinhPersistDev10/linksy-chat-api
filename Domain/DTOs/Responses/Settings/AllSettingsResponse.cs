namespace linksy_backend_api.Domain.DTOs.Responses.Settings
{
    public class AllSettingsResponse
    {
        public UserSettingsResponse? UserSettings { get; set; }
        public NotificationSettingsResponse? NotificationSettings { get; set; }
        public PrivacySettingsResponse? PrivacySettings { get; set; }
        public UserStatusResponse? UserStatus { get; set; }
    }
}
