using linksy_backend_api.Domain.DTOs.Responses.Messages;
using linksy_backend_api.DTOs.MessagesDTOs;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface IPollService
    {
        Task<PollResponse> VoteAsync(Guid userId, Guid messageId, VotePollRequest request);
        Task<PollResponse> CloseAsync(Guid userId, Guid messageId);
        Task<PollResponse?> GetPollAsync(Guid messageId, Guid userId);
        Task<Dictionary<Guid, PollResponse>> GetPollsByMessageIdsAsync(
            IEnumerable<Guid> messageIds,
            Guid userId);
    }
}
