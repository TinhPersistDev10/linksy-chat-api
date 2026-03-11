using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface IChatroomRepository : IRepository<Chatroom>
    {
        Task<List<Chatroom>> GetUserChatroomsAsync(Guid userId, bool includeArchived = false);
        Task<Chatroom?> GetWithMembersAsync(Guid chatroomId);
        Task<Chatroom?> GetWithMessagesAsync(Guid chatroomId, int messageCount = 50);
        Task<Chatroom?> GetDirectChatroomAsync(Guid user1Id, Guid user2Id);
        Task<List<Chatroom>> GetActiveChatroomsAsync(Guid userId);
        Task<bool> IsUserMemberAsync(Guid chatroomId, Guid userId);
    }
} 