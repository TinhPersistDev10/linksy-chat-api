using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        Task<List<MessageResponse>> GetRepliesAsync(Guid messageId);
        Task CreateMessageNotificationsAsync(Message message, Guid senderId);
        Task<MessageResponse> MapToMessageResponseAsync(Message message);
    }
}