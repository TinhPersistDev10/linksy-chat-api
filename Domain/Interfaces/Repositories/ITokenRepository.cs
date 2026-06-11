using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface ITokenRepository
    {
        Task<AccessToken?> GetActiveByTokenAsync(string token);
        Task<AccessToken?> GetActiveByRefreshTokenAsync(string refreshToken);
        Task CleanupExpiredAsync(Guid userId);
        Task RevokeAllByUserIdAsync(Guid userId);
    }
}