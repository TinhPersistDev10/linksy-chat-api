using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Repositories
{
    public class ChatroomMemberRepository : Repository<ChatroomMember>, IChatroomMemberRepository
    {
        private readonly LinksyDbContext _context;
        public ChatroomMemberRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ChatroomMember?> GetActiveMemberAsync(Guid chatroomId, Guid userId)
        {
            return await FirstOrDefaultAsync(m => m.ChatroomId == chatroomId && m.UserId == userId && m.LeftAt == null);
        }

        public async Task<List<ChatroomMember>> GetActiveMembersWithUserAsync(Guid chatroomId)
        {
            return await Query()
                .Where(m => m.ChatroomId == chatroomId && m.LeftAt == null)
                .Include(m => m.User)
                .ToListAsync();
        }


        public async Task<ChatroomMember?> GetNextMemberToPromoteAsync(Guid chatroomId, Guid excludeUserId)
        {
            return await Query()
            .Where(m => m.ChatroomId == chatroomId && m.UserId != excludeUserId && m.LeftAt == null)
            .OrderBy(m => m.JoinedAt)
            .FirstOrDefaultAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid chatroomId, Guid userId, DateTime lastReadAt)
        {
            return await _context.Messages
                .Where(m => m.ChatroomId == chatroomId
                && m.SenderId != userId
                && m.SentAt > lastReadAt
                && m.IsDeleted == null || m.IsDeleted == false)
                .CountAsync();

        }

        public async Task<bool> HasActiveMemberAsync(Guid chatroomId, Guid userId)
        {
            return await AnyAsync(m => m.ChatroomId == chatroomId
            && m.UserId == userId
            && m.LeftAt == null);
        }

        public async Task<bool> HasOtherAdminAsync(Guid chatroomId, Guid excludeUserId)
        {
            return await AnyAsync(m => m.ChatroomId == chatroomId
                                    && m.UserId != excludeUserId
                                    && m.MemberRole == "admin"
                                    && m.LeftAt == null);
        }
    }
}