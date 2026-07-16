using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IUserStatusRepository : IRepository<UserStatus>
    {
        Task<UserStatus?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<UserStatus> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<UserStatus>> GetOnlineStatusesAsync(List<Guid> userIds, CancellationToken cancellationToken = default);
    }
}
