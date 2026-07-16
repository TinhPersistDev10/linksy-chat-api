using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.DTOs.Responses.MessageAttachment;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Models;

namespace linksy_backend_api.Core.Interfaces.Services
{
    public interface IMessageService
    {
        Task<MessageResponse> SendMessageAsync(Guid userId, SendMessageRequest messageDto);
        Task<IEnumerable<MessageResponse>> GetMessagesAsync(Guid userId, Guid chatroomId, int page = 1, int pageSize = 50);
        Task DeleteMessageAsync(Guid userId, Guid messageId);
        Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, string newText);
        Task MarkMessageAsReadAsync(Guid userId, Guid chatroomId, Guid messageId);
        Task MarkAllMessagesAsReadAsync(Guid userId, Guid chatroomId);
        Task MarkMessageAsDeliveredAsync(Guid userId, Guid messageId);
        Task<List<MessageResponse>> GetRepliesAsync(Guid userId, Guid messageId);
        Task CreateMessageNotificationsAsync(Message message, Guid senderId);
        Task<UploadAttachmentResponse> UploadAttachmentAsync(Guid userId, Guid chatroomId, IFormFile file, string attachmentType);

        /// <summary>
        /// Persists a call summary as a real chat message (messageType "call_log") so it survives
        /// reload/tab switches, then broadcasts it through the normal "ReceiveMessage" event.
        /// Bypasses the permission/attachment checks in <see cref="SendMessageAsync"/> because
        /// this message is system-generated from a finished <c>CallLog</c>, not user input.
        /// </summary>
        Task<MessageResponse> CreateCallLogMessageAsync(
            Guid chatroomId,
            Guid callerId,
            Guid callLogId,
            string callType,
            string callStatus,
            int durationSec,
            DateTime startedAt,
            DateTime? endedAt);
    }
}
