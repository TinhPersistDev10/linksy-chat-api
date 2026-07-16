using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.Settings
{
    public class UpdatePrivacySettingsRequest
    {
        public bool? ReadReceiptsEnabled { get; set; }
        public bool? TypingIndicatorsEnabled { get; set; }
        public bool? LastSeenEnabled { get; set; }

        [RegularExpression("^(everyone|friends|nobody)$",
            ErrorMessage = "ProfilePhotoVisibility must be 'everyone', 'friends', or 'nobody'")]
        public string? ProfilePhotoVisibility { get; set; }

        [RegularExpression("^(everyone|friends|nobody)$",
            ErrorMessage = "StatusVisibility must be 'everyone', 'friends', or 'nobody'")]
        public string? StatusVisibility { get; set; }

        [RegularExpression("^(everyone|friends|nobody)$",
            ErrorMessage = "WhoCanAddToGroups must be 'everyone', 'friends', or 'nobody'")]
        public string? WhoCanAddToGroups { get; set; }
    }
}
