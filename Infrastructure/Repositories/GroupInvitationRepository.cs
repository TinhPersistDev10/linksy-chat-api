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
    public class GroupInvitationRepository : Repository<GroupInvitation>, IGroupInvitationRepository
    {
        private readonly LinksyDbContext _context;

        public GroupInvitationRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<GroupInvitation?> GetInvitationForUserAsync(Guid invitedUserId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await FirstOrDefaultAsync(i => i.InvitationId == invitedUserId
            && i.InvitedUserId == userId);
        }

        public async Task<GroupInvitation?> GetPendingInvitationsAsync(Guid chatroomId, Guid invitedUserId, CancellationToken cancellationToken = default)
        {
            return await FirstOrDefaultAsync(i => i.ChatroomId == chatroomId
            && i.InvitedUserId == invitedUserId
            && i.Status == "pending");
        }

        public async Task<List<GroupInvitation>> GetReceivedPendingInvitationsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await Query()
                .Where(i => i.InvitedUserId == userId
                         && i.Status == "pending"
                         && (i.ExpiresAt == null || i.ExpiresAt > DateTime.UtcNow))
                .Include(i => i.Chatroom)
                .Include(i => i.InvitedByNavigation)
                .OrderByDescending(i => i.SentAt)
                .ToListAsync();
        }
    }
}