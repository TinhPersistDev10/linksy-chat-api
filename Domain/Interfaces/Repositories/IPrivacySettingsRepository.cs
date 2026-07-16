using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IPrivacySettingsRepository : IRepository<PrivacySettings>
    {
        Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<PrivacySettings> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
