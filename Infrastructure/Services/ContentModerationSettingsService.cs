using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class ContentModerationSettingsService : IContentModerationSettingsService
    {
        private const int SettingsRowId = 1;

        private readonly LinksyDbContext _db;
        private readonly IContentModerationStateCache _stateCache;

        public ContentModerationSettingsService(
            LinksyDbContext db,
            IContentModerationStateCache stateCache)
        {
            _db = db;
            _stateCache = stateCache;
        }

        public async Task<ContentModerationSetting> GetAsync()
        {
            var settings = await _db.ContentModerationSettings
                .FirstOrDefaultAsync(s => s.Id == SettingsRowId);

            if (settings != null)
                return settings;

            settings = new ContentModerationSetting
            {
                Id = SettingsRowId,
                Enabled = true,
                BannedWords = new List<string>(),
                UpdatedAt = DateTime.UtcNow
            };
            _db.ContentModerationSettings.Add(settings);
            await _db.SaveChangesAsync();
            return settings;
        }

        public async Task<ContentModerationSetting> UpdateAsync(
            bool enabled, List<string>? bannedWords, Guid updatedByUserId)
        {
            var settings = await GetAsync();

            settings.Enabled = enabled;
            if (bannedWords != null)
                settings.BannedWords = bannedWords;
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedByUserId = updatedByUserId;

            await _db.SaveChangesAsync();
            await _stateCache.RefreshAsync();

            return settings;
        }
    }
}
