namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface IContentModerationService
    {
        bool ContainsBannedContent(string? text);
        void EnsureAllowed(string? text);
    }
}
