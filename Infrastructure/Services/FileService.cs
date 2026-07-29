using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Responses.MessageAttachment;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Formats.Jpeg;
using Size = SixLabors.ImageSharp.Size;

namespace linksy_backend_api.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<FileService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _logger = logger;
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName))
                throw new InvalidOperationException("Cloudinary CloudName is not configured");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Cloudinary ApiKey is not configured");
            if (string.IsNullOrWhiteSpace(apiSecret))
                throw new InvalidOperationException("Cloudinary ApiSecret is not configured");

            var account = new Account(cloudName, apiKey, apiSecret);
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
                    throw new InvalidOperationException("File không hợp lệ");

                var processedImage = await ProcessImageAsync(file, 1024, 1024, 92);
                using var stream = new MemoryStream(processedImage);

                // ── Dùng tên file unique để tránh conflict ──────────────────────
                var publicId = $"linksy/{folder}/{Guid.NewGuid()}";

                var uploadParams = new CloudinaryDotNet.Actions.ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = publicId,          // ← explicit publicId, bỏ Folder
                    Overwrite = true,
                    Transformation = new Transformation()
                        .Width(1024).Height(1024)
                        .Crop("fill")
                        .Gravity("face")
                        .Quality("auto:best")
                        .FetchFormat("auto"),
                    // ← KHÔNG đặt UploadPreset nếu dùng API Key/Secret (signed upload)
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                    throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");

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

        public async Task<byte[]> ProcessImageAsync(IFormFile file, int maxWidth = 400, int maxHeight = 400, int jpegQuality = 85)
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
                Quality = jpegQuality
            });

            return output.ToArray();
        }

        public Task<UploadAttachmentResponse> UploadMessageAttachmentAsync(IFormFile file, string attachmentType, Guid chatroomId)
        {
            if (file == null || file.Length == 0) throw new ArgumentNullException("File is required");
            attachmentType = attachmentType.Trim().ToLowerInvariant();
            _logger.LogInformation(
                "Upload file. FileName={FileName}, ContentType={ContentType}, Length={Length}",
                file.FileName,
                file.ContentType,
                file.Length);

            ValidateAttachment(file, attachmentType);
            return attachmentType switch
            {
                "image" => UploadMessageImageAsync(file, chatroomId),
                "video" => UploadMessageVideoAsync(file, chatroomId),
                "file" => UploadMessageRawFileAsync(file, chatroomId),
                _ => throw new ArgumentException("Invalid attachment type"),
            };
        }

        public async Task<UploadAttachmentResponse> UploadMessageImageAsync(IFormFile file, Guid chatroomId)
        {
            var publicId = $"linksy/messages/{chatroomId}/images/{Guid.NewGuid()}";
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.Name, stream),
                PublicId = publicId,
                Overwrite = false,
                Transformation = new Transformation()
                    .Quality("auto:good")
                    .FetchFormat("auto")
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null) throw new Exception($"Cloudinary upload error: {result.Error.Message}");
            return new UploadAttachmentResponse
            {
                AttachmentType = "image",
                FileName = file.FileName,
                CdnUrl = result.SecureUrl.ToString(),
                FilePath = result.PublicId,
                FileSize = result.Bytes,
                MimeType = file.ContentType,
                ThumbnailUrl = result.SecureUrl.ToString(),
                Width = result.Width,
                Height = result.Height,
                DurationMs = null
            };
        }

        public async Task<UploadAttachmentResponse> UploadMessageVideoAsync(
    IFormFile file,
    Guid chatroomId)
        {
            var publicId = $"linksy/messages/{chatroomId}/videos/{Guid.NewGuid()}";

            await using var stream = file.OpenReadStream();

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = publicId,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Cloudinary upload error: {result.Error.Message}");

            return new UploadAttachmentResponse
            {
                AttachmentType = "video",
                FileName = file.FileName,
                CdnUrl = result.SecureUrl.ToString(),
                FilePath = result.PublicId,
                FileSize = result.Bytes,
                MimeType = file.ContentType,
                ThumbnailUrl = BuildVideoThumbnailUrl(result.PublicId),
                Width = result.Width,
                Height = result.Height,
                DurationMs = result.Duration > 0
                    ? (int)(result.Duration * 1000)
                    : null
            };
        }

        private string BuildVideoThumbnailUrl(string publicId)
        {
            return _cloudinary.Api.UrlVideoUp
                .Transform(new Transformation()
                    .StartOffset("1")
                    .FetchFormat("jpg"))
                .BuildUrl(publicId + ".jpg");
        }
        public async Task<UploadAttachmentResponse> UploadMessageRawFileAsync(
            IFormFile file,
            Guid chatroomId)
        {
            var publicId = $"linksy/messages/{chatroomId}/files/{Guid.NewGuid()}";

            await using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = publicId,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Cloudinary upload error: {result.Error.Message}");

            return new UploadAttachmentResponse
            {
                AttachmentType = "file",
                FileName = file.FileName,
                CdnUrl = result.SecureUrl.ToString(),
                FilePath = result.PublicId,
                FileSize = result.Bytes,
                MimeType = file.ContentType,
                ThumbnailUrl = null,
                Width = null,
                Height = null,
                DurationMs = null
            };
        }
        private static void ValidateAttachment(IFormFile file, string attachmentType)
        {
            var contentType = (file.ContentType ?? string.Empty)
                .Split(';')[0]
                .Trim()
                .ToLowerInvariant();

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            var imageTypes = new[]
            {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

            var imageExtensions = new[]
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

            var videoTypes = new[]
            {
        "video/mp4",
        "video/webm",
        "video/quicktime"
    };

            var videoExtensions = new[]
            {
        ".mp4",
        ".webm",
        ".mov"
    };

            var fileTypes = new[]
            {
        "application/pdf",
        "text/plain",
        "text/yaml",
        "application/yaml",
        "application/x-yaml",
        "application/json",
        "text/csv",
        "application/xml",
        "text/xml",
        "application/zip",
        "application/x-zip-compressed",
        "application/octet-stream",
        "application/msword",
        "application/vnd.ms-excel",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

            var fileExtensions = new[]
            {
        ".pdf",
        ".txt",
        ".yaml",
        ".yml",
        ".json",
        ".xml",
        ".csv",
        ".zip",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx"
    };

            if (attachmentType == "image")
            {
                if (!imageTypes.Contains(contentType) && !imageExtensions.Contains(extension))
                    throw new ArgumentException("Invalid image type.");

                if (file.Length > 10 * 1024 * 1024)
                    throw new ArgumentException("Image file is too large.");
            }
            else if (attachmentType == "video")
            {
                if (!videoTypes.Contains(contentType) && !videoExtensions.Contains(extension))
                    throw new ArgumentException("Invalid video type.");

                if (file.Length > 100 * 1024 * 1024)
                    throw new ArgumentException("Video file is too large.");
            }
            else if (attachmentType == "file")
            {
                if (!fileTypes.Contains(contentType) && !fileExtensions.Contains(extension))
                    throw new ArgumentException("Invalid file type.");

                if (file.Length > 50 * 1024 * 1024)
                    throw new ArgumentException("File is too large.");
            }
            else
            {
                throw new ArgumentException("Invalid attachment type.");
            }
        }

    }
}