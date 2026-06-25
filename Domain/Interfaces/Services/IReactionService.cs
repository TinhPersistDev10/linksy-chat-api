using linksy_backend_api.Domain.DTOs.Requests.Reacions;
using linksy_backend_api.Domain.DTOs.Responses.Reactions;
using linksy_backend_api.DTOs;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface IReactionService
    {
        Task<ApiResponseDto> ToggleReactionAsync(Guid userId, Guid messageId, ToggleReactionRequest request);
        Task<MessageReactionsResponse> GetMessageReactionsAsync(Guid userId, Guid messageId);
        Task<Dictionary<Guid, MessageReactionsResponse>> GetBatchReactionsAsync(Guid userId, List<Guid> messageIds);
    }
}