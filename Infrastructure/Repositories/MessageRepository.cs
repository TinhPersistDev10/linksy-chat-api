using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        private readonly LinksyDbContext _context;
        public MessageRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Message>> GetChatroomMessagesAsync(Guid chatroomId, int page, int pageSize)
        {
            return await Query()
                .Include(m => m.Sender)
                .Where(m => m.ChatroomId == chatroomId && (m.IsDeleted == null || m.IsDeleted == false))
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Message?> GetLastMessageAsync(Guid chatroomId)
        {
            return await Query()
                .Where(m => m.ChatroomId == chatroomId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Message>> GetRepliesAsync(Guid parentMessageId)
        {
            return await Query()
                .Include(m => m.Sender)
                .Where(m => m.ParentMessageId == parentMessageId && (m.IsDeleted == null || m.IsDeleted == false))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid chatroomId, Guid userId, DateTime lastReadAt)
        {
            return await CountAsync(m => 
                m.ChatroomId == chatroomId && 
                m.SenderId != userId &&
                m.SentAt > lastReadAt &&
                (m.IsDeleted == null || m.IsDeleted == false));
        }

        public async Task<Message?> GetWithSenderAsync(Guid messageId)
        {
            return await Query()
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public Task<List<Message>> SearchMessageAsync(Guid chatroomId, string keyword, int limit = 50)
        {
            throw new NotImplementedException();
        }
    }
}