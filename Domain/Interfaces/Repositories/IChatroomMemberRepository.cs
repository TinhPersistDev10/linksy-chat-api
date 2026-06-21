using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IChatroomMemberRepository : IRepository<ChatroomMember>
    {
        Task<ChatroomMember?> GetActiveMemberAsync(Guid chatroomId,Guid userId);
        Task<List<Guid>> GetActiveMemberIdsExceptAsync(Guid chatroomId, Guid excludeUserId);
        Task<bool> HasActiveMemberAsync(Guid chatroomId, Guid userId);
        Task<bool> HasOtherAdminAsync(Guid chatroomId, Guid excludeUserId);
        Task<ChatroomMember?> GetNextMemberToPromoteAsync(Guid chatroomId, Guid excludeUserId);
        Task<List<ChatroomMember>> GetActiveMembersWithUserAsync(Guid chatroomId);
        Task<int> GetUnreadCountAsync(Guid chatroomId, Guid userId, DateTime lastReadAt);
    }
}

