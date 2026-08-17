using linksy_backend_api.Domain.DTOs.Responses.Stickers;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface IStickerService
    {
        Task<StickerResponse> CreateStickerAsync(Guid userId, IFormFile file);
        Task<List<StickerResponse>> GetMyStickersAsync(Guid userId);
        Task DeleteStickerAsync(Guid userId, Guid stickerId);
    }
}
