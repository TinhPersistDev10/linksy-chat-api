using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface INotificationSettingsRepository : IRepository<NotificationSettings>
    {
        Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<NotificationSettings> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<NotificationSettings>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    }
}
