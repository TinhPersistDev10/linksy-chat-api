using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface IFriendshipRepository : IRepository<Friendship>
    {
        Task<List<User>> GetUserFriendsAsync(Guid userId);
        Task<List<Guid>> GetFriendIdsAsync(Guid userId);
        Task<bool> AreFriendsAsync(Guid user1Id, Guid user2Id);
        Task<Friendship?> GetFriendshipAsync(Guid user1Id, Guid user2Id);
        Task<int> GetFriendsCountAsync(Guid userId);
    }
}