using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Domain.Options;
using Microsoft.Extensions.Options;

namespace linksy_backend_api.Infrastructure.Services
{
    public class ContentModerationService : IContentModerationService
    {
        public const string ViolationMessage =
            "Tin nhắn của bạn có từ khóa vi phạm tiêu chuẩn cộng đồng.";

        private readonly bool _enabled;
        private readonly HashSet<string> _bannedTokens;
        private readonly List<string> _bannedPhrases;

        public ContentModerationService(IOptions<ContentModerationOptions> options)
        {
            var config = options.Value ?? new ContentModerationOptions();
            _enabled = config.Enabled;

            var normalized = (config.BannedWords ?? new List<string>())
                .Select(Normalize)
                .Where(w => w.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            _bannedTokens = normalized
                .Where(w => !w.Contains(' ', StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);

            _bannedPhrases = normalized
                .Where(w => w.Contains(' ', StringComparison.Ordinal))
                .ToList();
        }

        public bool ContainsBannedContent(string? text)
        {
            if (!_enabled || string.IsNullOrWhiteSpace(text))
                return false;
            if (_bannedTokens.Count == 0 && _bannedPhrases.Count == 0)
                return false;

            var normalized = Normalize(text);
            if (normalized.Length == 0)
                return false;

            foreach (var phrase in _bannedPhrases)
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
                if (_bannedTokens.Contains(token))
                    return true;
            }

            return false;
        }

        public void EnsureAllowed(string? text)
        {
            if (ContainsBannedContent(text))
                throw new ArgumentException(ViolationMessage);
        }

        private static string Normalize(string input)
        {
            var lower = input.Trim().ToLowerInvariant()
                .Replace('đ', 'd')
                .Replace('Đ', 'd');

            var formD = lower.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;
                if (ch is '\u200B' or '\u200C' or '\u200D' or '\uFEFF')
                    continue;
                sb.Append(ch);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
