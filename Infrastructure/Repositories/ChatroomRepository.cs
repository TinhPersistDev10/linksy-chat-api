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

        public async Task<List<Chatroom>> GetUserChatroomsAsync(Guid userId, bool includeArchived = false, string? roomType = null)
        {
            var query = Query()
                .Include(c => c.ChatroomMembers.Where(rm => rm.LeftAt == null))
                .ThenInclude(rm => rm.User)
                .Include(c => c.LastMessage).ThenInclude(m => m!.Sender)
                .Where(c => c.ChatroomMembers.Any(rm => rm.UserId == userId && rm.LeftAt == null));

            // Hide conversations the user soft-deleted until newer activity arrives.
            query = query.Where(c => !c.ChatroomMembers.Any(rm =>
                rm.UserId == userId &&
                rm.LeftAt == null &&
                rm.ClearedAt != null &&
                (c.LastActivityAt == null || c.LastActivityAt <= rm.ClearedAt)));

            if (!includeArchived)
                query = query.Where(c => !(c.IsArchived ?? false));

            if (!string.IsNullOrEmpty(roomType))
                query = query.Where(c => c.RoomType == roomType);

            return await query
                .OrderByDescending(c => c.ChatroomMembers
                    .Where(rm => rm.UserId == userId && rm.LeftAt == null)
                    .Select(rm => rm.IsPinned)
                    .FirstOrDefault())
                .ThenByDescending(c => c.ChatroomMembers
                    .Where(rm => rm.UserId == userId && rm.LeftAt == null)
                    .Select(rm => rm.PinnedAt)
                    .FirstOrDefault())
                .ThenByDescending(c => c.LastActivityAt)
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

        public async Task<bool> IsUserMemberAsync(Guid chatroomId, Guid userId)
        {
            return await _context.ChatroomMembers
                .AnyAsync(rm => rm.ChatroomId == chatroomId &&
                    rm.UserId == userId &&
                    rm.LeftAt == null);
        }

        public async Task<Chatroom?> GetChatroomDetailsAsync(Guid chatroomId)
        {
            return await Query()
                .Include(c => c.ChatroomMembers.Where(rm => rm.LeftAt == null))
                    .ThenInclude(cm => cm.User)
                .Include(c => c.LastMessage)
                    .ThenInclude(m => m!.Sender)
                .FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
        }

        public async Task<Guid[]> GetActiveMemberIdsAsync(Guid chatroomId)
        {
            return await _context.ChatroomMembers
           .Where(m => m.ChatroomId == chatroomId && m.LeftAt == null)
           .Select(m => m.UserId)
           .ToArrayAsync();
        }
    }
}