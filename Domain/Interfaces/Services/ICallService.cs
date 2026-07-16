using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface ICallService
    {
        Task<CallLog> InitiateCallAsync(Guid callerId, Guid chatroomId, string callType);
        Task<CallLog> AnswerCallAsync(Guid callLogId, Guid userId);
        Task<CallLog> RejectCallAsync(Guid callLogId, Guid userId);
        Task<CallLog> EndCallAsync(Guid callLogId, Guid userId);
        Task<CallLog> JoinCallAsync(Guid callLogId, Guid userId);
        Task<CallLog> LeaveCallAsync(Guid callLogId, Guid userId);
        Task<CallLog> GetCallForParticipantAsync(Guid callLogId, Guid userId);
        Task<CallLog?> GetActiveCallForChatroomAsync(Guid chatroomId, Guid userId);
        Task<IReadOnlyList<CallLog>> HandleParticipantDisconnectedAsync(Guid userId);
        Task EnsureCanSignalAsync(Guid callLogId, Guid senderId, Guid recipientId);
    }
}
