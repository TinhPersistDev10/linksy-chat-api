using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Requests.MessageAttachment
{
    public class UploadAttachmentRequest
    {
        
    public IFormFile File { get; set; } = null!;
    public Guid ChatroomId { get; set; }
    public string AttachmentType { get; set; } = string.Empty;
    }
}