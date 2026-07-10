using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Requests
{
    public class SendMessageAttachmentRequest
    {
        [Required]
        public string AttachmentType { get; set; } = string.Empty; // image | video | file | audio

        public string? FileName { get; set; }

        [Required]
        public string CdnUrl { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public long? FileSize { get; set; }

        public string? MimeType { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? DurationMs { get; set; }
    }
}