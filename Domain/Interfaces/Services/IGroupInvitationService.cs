using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.ChatroomDTO;

namespace linksy_backend_api.Core.Interfaces.Services
{
    public interface IGroupInvitationService
    {
        Task<ApiResponseDto> SendGroupInvitationAsync(Guid userId, Guid chatroomId, SendGroupInvitationRequest request);
        Task<ApiResponseDto> AcceptGroupInvitationAsync(Guid userId, Guid invitationId);
        Task<ApiResponseDto> RejectGroupInvitationAsync(Guid userId, Guid invitationId);
        Task<List<GroupInvitationRequest>> GetReceivedInvitationsAsync(Guid userId);
    }
}