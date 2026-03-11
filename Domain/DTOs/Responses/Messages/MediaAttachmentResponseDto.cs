using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Responses.Messages
{
    public class MediaAttachmentResponseDto
    {
        public Guid AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? Duration { get; set; }
    }
}