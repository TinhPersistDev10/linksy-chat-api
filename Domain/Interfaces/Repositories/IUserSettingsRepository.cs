using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IUserSettingsRepository : IRepository<UserSettings>
    {
        Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<UserSettings> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
