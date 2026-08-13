using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class FriendshipRepository : Repository<Friendship>, IFriendshipRepository
    {
        private readonly LinksyDbContext _context;
        // private readonly ILogger<FriendshipRepository> _logger;
        public FriendshipRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
            // _logger = logger;
        }

        public async Task<bool> AreFriendsAsync(Guid user1Id, Guid user2Id)
        {
            var minId = user1Id < user2Id ? user1Id : user2Id;
            var maxId = user1Id < user2Id ? user2Id : user1Id;

            return await AnyAsync(f => f.User1Id == minId && f.User2Id == maxId);

        }

        public async Task<int> GetFriendsCountAsync(Guid userId)
        {
            return await CountAsync(f => f.User1Id == userId || f.User2Id == userId);
        }

        public async Task<Friendship?> GetFriendshipAsync(Guid user1Id, Guid user2Id)
        {
            var minId = user1Id < user2Id ? user1Id : user2Id;
            var maxId = user1Id < user2Id ? user2Id : user1Id;

            return await FirstOrDefaultAsync(f => f.User1Id == minId && f.User2Id == maxId);
        }


        public async Task<List<User>> GetUserFriendsAsync(Guid userId)
        {
            var friendships = await Query()
                .Include(f => f.User1)
                .Include(f => f.User2)
                .Where(f => f.User1Id == userId || f.User2Id == userId)
                .ToListAsync();

            return friendships.Select(f => f.User1Id == userId ? f.User2 : f.User1)
                    .ToList();
        }

        public async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
        {
            return await Query()
                .Where(f => f.User1Id == userId || f.User2Id == userId)
                .Select(f => f.User1Id == userId ? f.User2Id : f.User1Id)
                .ToListAsync();
        }
    }
}