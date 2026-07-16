using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface IChatroomRepository : IRepository<Chatroom>
    {
        // Chatroom queries
        Task<Chatroom?> GetDirectChatroomAsync(Guid user1Id, Guid user2Id);
        Task<Chatroom?> GetChatroomDetailsAsync(Guid chatroomId);
        Task<List<Chatroom>> GetUserChatroomsAsync(Guid userId, bool includeArchived = false, string? roomType = null);
        Task<bool> IsUserMemberAsync(Guid chatroomId, Guid userId);

        Task<Guid[]> GetActiveMemberIdsAsync(Guid chatroomId);
    }
}