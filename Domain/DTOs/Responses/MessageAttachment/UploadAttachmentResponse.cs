using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Responses.MessageAttachment
{
    public class UploadAttachmentResponse
    {
        public string AttachmentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string CdnUrl { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? DurationMs { get; set; }
    }
}