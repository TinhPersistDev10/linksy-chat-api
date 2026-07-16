namespace linksy_backend_api.Domain.DTOs.Requests.Settings
{
    public class UpdateNotificationSettingsRequest
    {
        public bool? NotificationsEnabled { get; set; }
        public bool? NotificationSoundEnabled { get; set; }
        public bool? MessagePreviewEnabled { get; set; }
        public bool? EmailNotifications { get; set; }
    }
}
