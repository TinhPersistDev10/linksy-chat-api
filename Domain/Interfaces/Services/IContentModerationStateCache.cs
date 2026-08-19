using linksy_backend_api.Domain.Options;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    /// <summary>
    /// Holds the currently active, parsed content moderation state in memory so
    /// ContentModerationService can check messages synchronously without hitting the DB.
    /// RefreshAsync reloads it from the persisted settings; call it right after an admin update
    /// so the change takes effect immediately, with no restart required.
    /// </summary>
    public interface IContentModerationStateCache
    {
        ContentModerationState Current { get; }
        Task RefreshAsync();
    }
}
