using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Responses.Stickers;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class StickerService : IStickerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly ILogger<StickerService> _logger;

        public StickerService(
            IUnitOfWork unitOfWork,
            IFileService fileService,
            ILogger<StickerService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<StickerResponse> CreateStickerAsync(Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File sticker không được để trống.");

            var uploadResult = await _fileService.UploadStickerAsync(file, userId);

            var sticker = new Sticker
            {
                Id = Guid.NewGuid(),
                OwnerId = userId,
                ImageUrl = uploadResult.CdnUrl,
                PublicId = uploadResult.FilePath ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Stickers.AddAsync(sticker);
            await _unitOfWork.SaveChangesAsync();

            return ToResponse(sticker);
        }

        public async Task<List<StickerResponse>> GetMyStickersAsync(Guid userId)
        {
            var stickers = await _unitOfWork.Stickers.Query()
                .Where(s => s.OwnerId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return stickers.Select(ToResponse).ToList();
        }

        public async Task DeleteStickerAsync(Guid userId, Guid stickerId)
        {
            var sticker = await _unitOfWork.Stickers.FirstOrDefaultAsync(s => s.Id == stickerId);
            if (sticker == null)
                throw new KeyNotFoundException("Không tìm thấy sticker.");

            if (sticker.OwnerId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền xoá sticker này.");

            _unitOfWork.Stickers.Remove(sticker);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _fileService.DeleteAvatarAsync(sticker.ImageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete sticker asset from Cloudinary. StickerId={StickerId}", stickerId);
            }
        }

        private static StickerResponse ToResponse(Sticker sticker)
        {
            return new StickerResponse
            {
                Id = sticker.Id,
                ImageUrl = sticker.ImageUrl,
                CreatedAt = sticker.CreatedAt
            };
        }
    }
}
