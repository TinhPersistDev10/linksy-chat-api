using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class MessageAttachment
    {
        public Guid AttachmentId { get; set; }
        public Guid MessageId { get; set; }
        public string AttachmentType { get; set; } = null!;  // image | video | audio | file | sticker
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public string? CdnUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? DurationMs { get; set; }   // milliseconds for audio/video
        public DateTime UploadedAt { get; set; }

        // Navigation
        public virtual Message Message { get; set; } = null!;
    }
}