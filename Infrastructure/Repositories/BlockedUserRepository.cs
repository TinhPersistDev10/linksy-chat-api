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
        // private readonly ILogger<BlockedUserRepository> _logger;

        public BlockedUserRepository(LinksyDbContext context) : base(context)
        {
            // _logger = logger;
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
            catch (System.Exception ex)
            {
                // _logger.LogError(ex, "Error checking if {User1Id} and {User2Id} are blocked",
                //                     user1Id, user2Id);
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
                // _logger.LogError(ex, "Error counting blocked users for {UserId}", userId);
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

                // _logger.LogError(ex, "Error getting block from {BlockerId} to {BlockedUserId}",
                //     blockerId, blockedUserId);
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

                // _logger.LogError(ex, "Error getting users who blocked {UserId}", userId);
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
                //     _logger.LogError(ex, "Error getting blocked users for {UserId}", userId);
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
                // _logger.LogError(ex, "Error getting blocked users with details for {UserId}", userId);
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
                // _logger.LogError(ex, "Error checking if {BlockerId} has blocked {BlockedUserId}", blockerId, blockedUserId);
                throw;
            }
        }
    }
}