using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Domain.Options;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class ContentModerationStateCache : IContentModerationStateCache
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private volatile ContentModerationState _current = ContentModerationState.Build(true, null);

        public ContentModerationStateCache(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public ContentModerationState Current => _current;

        public async Task RefreshAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LinksyDbContext>();

            var settings = await db.ContentModerationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            _current = ContentModerationState.Build(
                settings?.Enabled ?? true,
                settings?.BannedWords);
        }
    }
}
