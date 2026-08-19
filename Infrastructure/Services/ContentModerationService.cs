using System.Text.RegularExpressions;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Domain.Options;

namespace linksy_backend_api.Infrastructure.Services
{
    public class ContentModerationService : IContentModerationService
    {
        public const string ViolationMessage =
            "Tin nhắn của bạn có từ khóa vi phạm tiêu chuẩn cộng đồng.";

        private readonly IContentModerationStateCache _stateCache;

        public ContentModerationService(IContentModerationStateCache stateCache)
        {
            _stateCache = stateCache;
        }

        public bool ContainsBannedContent(string? text)
        {
            var state = _stateCache.Current;

            if (!state.Enabled || string.IsNullOrWhiteSpace(text))
                return false;
            if (state.BannedTokens.Count == 0 && state.BannedPhrases.Count == 0)
                return false;

            var normalized = ContentModerationState.Normalize(text);
            if (normalized.Length == 0)
                return false;

            foreach (var phrase in state.BannedPhrases)
            {
                var pattern = string.Join(
                    @"[^\p{L}\p{N}]+",
                    phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(Regex.Escape));
                if (Regex.IsMatch(normalized, $@"(^|[^\p{{L}}\p{{N}}]){pattern}([^\p{{L}}\p{{N}}]|$)"))
                    return true;
            }

            var tokens = Regex.Split(normalized, @"[^\p{L}\p{N}]+")
                .Where(t => t.Length > 0);

            foreach (var token in tokens)
            {
                if (state.BannedTokens.Contains(token))
                    return true;
            }

            return false;
        }

        public void EnsureAllowed(string? text)
        {
            if (ContainsBannedContent(text))
                throw new ArgumentException(ViolationMessage);
        }
    }
}
