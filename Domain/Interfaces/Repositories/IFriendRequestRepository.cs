using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface IFriendRequestRepository : IRepository<FriendRequest>
    {
        Task<List<FriendRequest>> GetPendingRequestsAsync(Guid userId);
        Task<List<FriendRequest>> GetSentRequestsAsync(Guid userId);
        Task<FriendRequest?> GetRequestAsync(Guid senderId, Guid receiverId);
        Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId);
    }
}