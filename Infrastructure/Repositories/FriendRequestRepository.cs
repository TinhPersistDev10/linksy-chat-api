using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class FriendRequestRepository : Repository<FriendRequest>, IFriendRequestRepository
    {
        private readonly LinksyDbContext _context;
        // private readonly ILogger<UnitOfWork> _logger;

        public FriendRequestRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
            // _logger = logger;
        }   

        public async Task<List<FriendRequest>> GetPendingRequestsAsync(Guid userId)
        {
            return await Query()
                .AsNoTracking()
                .Where(fr => fr.ReceiverId == userId && fr.Status == "pending")
                .OrderByDescending(fr => fr.SentAt)
                .ToListAsync();
        }

        public async Task<FriendRequest?> GetRequestAsync(Guid senderId, Guid receiverId)
        {
            try
            {
                return await _context.FriendRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(fr =>
                        fr.SenderId == senderId &&
                        fr.ReceiverId == receiverId );
                        // && fr.Status == "pending"
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error in GetRequestAsync for sender {SenderId}, receiver {ReceiverId}", senderId, receiverId);
                throw;
            }
        }

        public async Task<List<FriendRequest>> GetSentRequestsAsync(Guid userId)
        {
            return await _context.FriendRequests
            .AsNoTracking()
            .Where(fr => fr.SenderId == userId && fr.Status == "pending")
            .OrderByDescending(fr => fr.SentAt)
            .ToListAsync();
        }

        public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
        {
            return await _context.FriendRequests
           .AsNoTracking()
           .AnyAsync(fr =>
               fr.SenderId == senderId &&
               fr.ReceiverId == receiverId &&
               fr.Status == "pending");
        }
    }
}