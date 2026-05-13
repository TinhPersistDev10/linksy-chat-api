using linksy_backend_api.Domain.DTOs.Requests.Reacions;
using linksy_backend_api.Domain.DTOs.Responses.Reactions;
using linksy_backend_api.DTOs;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface IReactionService
    {
        /// <summary>Toggle reaction — thêm nếu chưa có, xóa nếu đã có cùng emoji.</summary>
        Task<ApiResponseDto> ToggleReactionAsync(Guid userId, Guid messageId, ToggleReactionRequest request);

        /// <summary>Lấy tất cả reactions của một message, gom nhóm theo emoji.</summary>
        Task<MessageReactionsResponse> GetMessageReactionsAsync(Guid userId, Guid messageId);

        /// <summary>Lấy tất cả reactions của nhiều messages cùng lúc (batch).</summary>
        Task<Dictionary<Guid, MessageReactionsResponse>> GetBatchReactionsAsync(Guid userId, List<Guid> messageIds);
    }
}