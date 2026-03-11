  using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class ChatroomRepository : Repository<Chatroom>, IChatroomRepository
    {
        private readonly LinksyDbContext _context;
        public ChatroomRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Chatroom>> GetUserChatroomsAsync(Guid userId, bool includeArchived = false)
        {
            var query = _context.Chatrooms
           .Include(c => c.ChatroomMembers)
               .ThenInclude(rm => rm.User)
           .Include(c => c.LastMessage)
               .ThenInclude(m => m.Sender)
           .Where(c => c.ChatroomMembers.Any(rm =>
               rm.UserId == userId &&
               rm.LeftAt == null));

            if (!includeArchived)
                query = query.Where(c => !(c.IsArchived ?? false));

            return await query
                .OrderByDescending(c => c.LastActivityAt)
                .ToListAsync();
        }
        public async Task<List<Chatroom>> GetActiveChatroomsAsync(Guid userId)
        {
            return await Query()
                .Where(c => c.IsActive.GetValueOrDefault() &&
                    !(c.IsArchived ?? false) &&
                    c.ChatroomMembers.Any(rm => rm.UserId == userId && rm.LeftAt == null))
                .OrderByDescending(c => c.LastActivityAt)
                .ToListAsync();
        }

        public async Task<Chatroom?> GetDirectChatroomAsync(Guid user1Id, Guid user2Id)
        {
            return await Query()
                .Include(c => c.ChatroomMembers)
                .Where(c => c.RoomType == "direct")
                .FirstOrDefaultAsync(c =>
                    c.ChatroomMembers.Any(rm => rm.UserId == user1Id && rm.LeftAt == null) &&
                    c.ChatroomMembers.Any(rm => rm.UserId == user2Id && rm.LeftAt == null));
        }
        public async Task<Chatroom?> GetWithMembersAsync(Guid chatroomId)
        {
            return await Query()
                .Include(c => c.ChatroomMembers)
                    .ThenInclude(rm => rm.User)
                .FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
        }

        public async Task<Chatroom?> GetWithMessagesAsync(Guid chatroomId, int messageCount = 50)
        {
            return await Query()
                .Include(c => c.Messages
                    .Where(m => !m.IsDeleted.GetValueOrDefault())
                    .OrderByDescending(m => m.SentAt)
                    .Take(messageCount))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
        }

        public async Task<bool> IsUserMemberAsync(Guid chatroomId, Guid userId)
        {
            return await _context.ChatroomMembers
                .AnyAsync(rm => rm.ChatroomId == chatroomId &&
                    rm.UserId == userId &&
                    rm.LeftAt == null);
        }
    }
}