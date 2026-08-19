using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface IContentModerationSettingsService
    {
        Task<ContentModerationSetting> GetAsync();

        /// <summary>
        /// Updates the moderation settings. Pass bannedWords = null to keep the current list
        /// and only change Enabled.
        /// </summary>
        Task<ContentModerationSetting> UpdateAsync(bool enabled, List<string>? bannedWords, Guid updatedByUserId);
    }
}
