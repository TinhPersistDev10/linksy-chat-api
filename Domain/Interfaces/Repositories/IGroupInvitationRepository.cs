using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IGroupInvitationRepository: IRepository<GroupInvitation>
    {
      Task<GroupInvitation?> GetPendingInvitationsAsync(Guid chatroomId, Guid invitedUserId, CancellationToken cancellationToken = default);
      Task<GroupInvitation?> GetInvitationForUserAsync(Guid invitedUserId, Guid userId, CancellationToken cancellationToken = default);
      Task<List<GroupInvitation>> GetReceivedPendingInvitationsAsync(Guid userId, CancellationToken cancellationToken = default); 
    }
}