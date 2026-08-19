using System;
using System.Collections.Generic;

namespace linksy_backend_api.Core.DTOs.AdminDTOs
{
    public class ContentModerationSettingsDto
    {
        public bool Enabled { get; set; }
        public List<string> BannedWords { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }
}
