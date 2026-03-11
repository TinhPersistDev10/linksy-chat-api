using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Core.Interfaces.Repositories
{
    public interface IBlockedUserRepository : IRepository<BlockedUser>
    {
        //Check user1 blocked user2
        Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedUserId);
        Task<bool> AreBlockedAsync(Guid user1Id, Guid user2Id);
        Task<BlockedUser?> GetBlockAsync(Guid blockerId, Guid blockedUserId);

        Task<List<BlockedUser>> GetBlockedUsersAsync(Guid userId);
        Task<List<BlockedUser>> GetBlockedUsersWithDetailsAsync(Guid userId);
        Task<List<BlockedUser>> GetBlockedByUsersAsync(Guid userId);
        Task<int> CountBlockedUsersAsync(Guid userId);
    }
}