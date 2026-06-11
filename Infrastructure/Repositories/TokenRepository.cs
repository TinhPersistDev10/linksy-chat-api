using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Infrastructure.Repositories
{
    public class TokenRepository : Repository<AccessToken>, ITokenRepository
    {
        private readonly LinksyDbContext _context;

        public TokenRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task CleanupExpiredAsync(Guid userId)
        {
            var expired = await FindAsync(t =>
            t.UserId == userId &&
            (t.RefreshTokenExpiresAt < DateTime.UtcNow || t.IsRevoked));
            RemoveRange(expired);
        }

        public async Task<AccessToken?> GetActiveByRefreshTokenAsync(string refreshToken)
            => await FirstOrDefaultAsync(t =>
            t.RefreshToken == refreshToken &&
            !t.IsRevoked &&
            t.RefreshTokenExpiresAt > DateTime.UtcNow);

        public async Task<AccessToken?> GetActiveByTokenAsync(string token)
            => await FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked);

        public async Task RevokeAllByUserIdAsync(Guid userId)
        {
            var tokens = await FindAsync(t => t.UserId == userId && !t.IsRevoked);
            foreach (var t in tokens) t.IsRevoked = true;
        }

    }
}
