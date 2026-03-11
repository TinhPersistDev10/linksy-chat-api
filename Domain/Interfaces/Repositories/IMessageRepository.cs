using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<List<Message>> GetChatroomMessagesAsync(Guid chatroomId, int page, int pageSize);
        Task<Message?> GetWithSenderAsync(Guid messageId);
        Task<List<Message>> GetRepliesAsync(Guid parentMessageId);
        Task<int> GetUnreadCountAsync(Guid chatroomId, Guid userId, DateTime lastReadAt);
        Task<Message?> GetLastMessageAsync(Guid chatroomId);
        Task<List<Message>> SearchMessageAsync (Guid chatroomId, string keyword, int limit = 50);
    }
}