using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.Settings
{
    public class UpdateUserStatusRequest
    {
        [Required]
        [RegularExpression("^(online|away|busy|offline)$",
            ErrorMessage = "StatusType must be 'online', 'away', 'busy', or 'offline'")]
        public string StatusType { get; set; } = "online";

        [StringLength(100)]
        public string? CustomStatus { get; set; }
    }
}
