using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface ICallService
    {
        Task<CallLog> InitiateCallAsync(Guid callerId, Guid chatroomId, string callType);
        Task<CallLog> AnswerCallAsync(Guid callLogId, Guid userId);
        Task<CallLog> RejectCallAsync(Guid callLogId, Guid userId);
        Task<CallLog> EndCallAsync(Guid callLogId, Guid userId);
        Task EnsureCanSignalAsync(Guid callLogId, Guid senderId, Guid recipientId);
    }
}
