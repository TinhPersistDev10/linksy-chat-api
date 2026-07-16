using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.Settings
{
    public class UpdateUserSettingsRequest
    {
        [StringLength(10)]
        public string? Language { get; set; }

        [StringLength(50)]
        public string? Timezone { get; set; }

        [StringLength(10)]
        public string? Theme { get; set; } // light | dark | system
    }
}
