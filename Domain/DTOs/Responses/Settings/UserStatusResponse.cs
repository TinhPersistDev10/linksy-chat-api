namespace linksy_backend_api.Domain.DTOs.Responses.Settings
{
    public class UserStatusResponse
    {
        public Guid StatusId { get; set; }
        public string StatusType { get; set; } = "offline";
        public string? CustomStatus { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
