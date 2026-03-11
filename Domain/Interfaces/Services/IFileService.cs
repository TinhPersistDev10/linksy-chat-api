using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.Interfaces.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Upload avatar và trả về URL
        /// </summary>
        Task<string> UploadAvatarAsync(IFormFile file, string folder = "avatars");
        
        /// <summary>
        /// Xóa avatar cũ
        /// </summary>
        Task<bool> DeleteAvatarAsync(string avatarUrl);
        
        /// <summary>
        /// Validate file avatar
        /// </summary>
        bool IsValidImage(IFormFile file);
        
        /// <summary>
        /// Resize và optimize image
        /// </summary>
        Task<byte[]> ProcessImageAsync(IFormFile file, int maxWidth = 400, int maxHeight = 400);
    }
}