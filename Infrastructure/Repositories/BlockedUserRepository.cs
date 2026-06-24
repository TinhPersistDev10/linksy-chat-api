using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Repositories
{
    public class BlockedUserRepository : Repository<BlockedUser>, IBlockedUserRepository
    {
        private readonly LinksyDbContext _context;

        public BlockedUserRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> AreBlockedAsync(Guid user1Id, Guid user2Id)
        {
            try
            {
                return await _context.BlockedUsers
                    .AsNoTracking()
                    .AnyAsync(b => (b.BlockerUserId == user1Id &&
                        b.BlockedUserId == user2Id) ||
                        (b.BlockerUserId == user2Id &&
                        b.BlockedUserId == user1Id));
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> CountBlockedUsersAsync(Guid userId)
        {
            try
            {
                return await _context.BlockedUsers
                .AsNoTracking()
                .CountAsync(b => b.BlockedUserId == userId);
            }
            catch
            {
                throw;
            }
        }

        public async Task<BlockedUser?> GetBlockAsync(Guid blockerId, Guid blockedUserId)
        {
            try
            {
                return await _context.BlockedUsers
                .FirstOrDefaultAsync(b =>
                b.BlockerUserId == blockerId &&
                b.BlockedUserId == blockedUserId);

            }
            catch
            {
                throw;
            }
        }

        public async Task<List<BlockedUser>> GetBlockedByUsersAsync(Guid userId)
        {
            try
            {
                return await _context.BlockedUsers
                .Include(b => b.BlockerUser)
                .Where(b => b.BlockedUserId == userId)
                .OrderByDescending(b => b.BlockedAt)
                .ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<BlockedUser>> GetBlockedUsersAsync(Guid userId)
        {
            try
            {
                return await _context.BlockedUsers
                .AsNoTracking()
                .Where(b => b.BlockerUserId == userId)
                .OrderByDescending(b => b.BlockedAt)
                .ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<BlockedUser>> GetBlockedUsersWithDetailsAsync(Guid userId)
        {
            try
            {
                return await _context.BlockedUsers
                .Include(b => b.BlockedUserNavigation)
                .Where(b => b.BlockerUserId == userId)
                .OrderByDescending(b => b.BlockedAt)
                .ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedUserId)
        {
            try
            {
                return await _context.BlockedUsers
                    .AsNoTracking()
                    .AnyAsync(b => b.BlockerUserId == blockerId && b.BlockedUserId == blockedUserId);
            }
            catch
            {
                throw;
            }
        }
    }
}