using linksy_backend_api.Domain.DTOs.Responses.MessageAttachment;

namespace linksy_backend_api.Core.Interfaces.Services
{
    public interface IFileService
    {
        Task<string> UploadAvatarAsync(IFormFile file, string folder = "avatars");
        Task<bool> DeleteAvatarAsync(string avatarUrl);
        bool IsValidImage(IFormFile file);
        Task<byte[]> ProcessImageAsync(IFormFile file, int maxWidth = 400, int maxHeight = 400);
        Task<UploadAttachmentResponse> UploadMessageAttachmentAsync(
            IFormFile file,
            string attachmentType,
            Guid chatroomId);
        Task<UploadAttachmentResponse>UploadMessageImageAsync(IFormFile file, Guid chatroomId);
        Task<UploadAttachmentResponse> UploadMessageVideoAsync(IFormFile file, Guid chatroomId);
        Task<UploadAttachmentResponse> UploadMessageRawFileAsync(IFormFile file, Guid chatroomId);
    }
}