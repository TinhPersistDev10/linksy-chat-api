using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using linksy_backend_api.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace linksy_backend_api.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<FileService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _logger = logger;
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account)
            {
                Api = { Secure = true }
            };
        }

        public async Task<string> UploadAvatarAsync(IFormFile file, string folder = "avatars")
        {
            try
            {
                if (!IsValidImage(file))
                {
                    throw new InvalidOperationException("File không hợp lệ");
                }

                var processedImage = await ProcessImageAsync(file);
                using var stream = new MemoryStream(processedImage);
                var uploadParams = new CloudinaryDotNet.Actions.ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"linksy/{folder}",
                    Transformation = new Transformation()
                    .Width(400).Height(400)
                    .Crop("fill") 
                    .Gravity("face")
                    .Quality("auto:good")
                    .FetchFormat("auto"),
                    Overwrite = true
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                    throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");

                // Trả về Secure URL (https)
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar to Cloudinary");
                throw;
            }
        }

        public async Task<bool> DeleteAvatarAsync(string avatarUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(avatarUrl))
                    return false;
                if (!avatarUrl.Contains("cloudinary.com"))
                    return false;

                // Lấy public_id từ URL
                var publicId = ExtractPublicId(avatarUrl);
                if (string.IsNullOrEmpty(publicId))
                    return false;
                var deleteParams = new CloudinaryDotNet.Actions.DeletionParams(publicId);
                var deleteResult = await _cloudinary.DestroyAsync(deleteParams);
                if (deleteResult.Error != null)
                {
                    _logger.LogError("Cloudinary delete error: {ErrorMessage}", deleteResult.Error.Message);
                    return false;
                }

                return deleteResult.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar: {AvatarUrl}", avatarUrl);
                return false;
            }
        }

        private string? ExtractPublicId(string avatarUrl)
        {
            try
            {
                var uri = new Uri(avatarUrl);
                var path = uri.AbsolutePath;
                var uploadIndex = path.IndexOf("/upload/");
                if (uploadIndex < 0) return null;

                var afterUpload = path[(uploadIndex + 8)..];

                // Bỏ version prefix (v1234/)
                if (afterUpload.StartsWith("v") && afterUpload.Contains("/"))
                    afterUpload = afterUpload[(afterUpload.IndexOf('/') + 1)..];

                // Bỏ extension
                var dotIndex = afterUpload.LastIndexOf('.');
                if (dotIndex > 0)
                    afterUpload = afterUpload[..dotIndex];

                return afterUpload;
            }
            catch
            {
                return null;
            }
        }

        public bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException($"File quá lớn. Tối đa: {MaxFileSize / 1024 / 1024}MB");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new InvalidOperationException($"Định dạng không hợp lệ. Chỉ chấp nhận: {string.Join(", ", _allowedExtensions)}");

            return true;
        }

        public async Task<byte[]> ProcessImageAsync(IFormFile file, int maxWidth = 400, int maxHeight = 400)
        {
            using var image = await Image.LoadAsync(file.OpenReadStream());

            // Resize nếu ảnh lớn hơn kích thước cho phép
            if (image.Width > maxWidth || image.Height > maxHeight)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(maxWidth, maxHeight),
                    Mode = ResizeMode.Max
                }));
            }

            // Convert sang JPEG và compress
            using var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder
            {
                Quality = 85 // Chất lượng 85%
            });

            return output.ToArray();
        }
    }
}