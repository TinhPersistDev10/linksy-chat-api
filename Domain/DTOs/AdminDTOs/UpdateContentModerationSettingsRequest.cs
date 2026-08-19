using System.Collections.Generic;

namespace linksy_backend_api.Core.DTOs.AdminDTOs
{
    public class UpdateContentModerationSettingsRequest
    {
        public bool Enabled { get; set; }

        /// <summary>Null keeps the current banned-word list; only Enabled changes.</summary>
        public List<string>? BannedWords { get; set; }
    }
}
