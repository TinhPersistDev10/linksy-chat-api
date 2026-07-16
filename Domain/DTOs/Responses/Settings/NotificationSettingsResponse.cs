namespace linksy_backend_api.Domain.DTOs.Responses.Settings
{
    public class NotificationSettingsResponse
    {
        public Guid Id { get; set; }
        public bool NotificationsEnabled { get; set; }
        public bool NotificationSoundEnabled { get; set; }
        public bool MessagePreviewEnabled { get; set; }
        public bool EmailNotifications { get; set; }
    }
}
