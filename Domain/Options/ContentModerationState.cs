using System.Globalization;
using System.Text;

namespace linksy_backend_api.Domain.Options
{
    /// <summary>
    /// Parsed, ready-to-match snapshot of the content moderation configuration.
    /// Rebuilt whenever the underlying settings change (see IContentModerationStateCache).
    /// </summary>
    public sealed class ContentModerationState
    {
        private static readonly HashSet<char> ZeroWidthChars = new()
        {
            (char)0x200B, (char)0x200C, (char)0x200D, (char)0xFEFF
        };

        public bool Enabled { get; }
        public IReadOnlySet<string> BannedTokens { get; }
        public IReadOnlyList<string> BannedPhrases { get; }

        private ContentModerationState(bool enabled, HashSet<string> bannedTokens, List<string> bannedPhrases)
        {
            Enabled = enabled;
            BannedTokens = bannedTokens;
            BannedPhrases = bannedPhrases;
        }

        public static ContentModerationState Build(bool enabled, IEnumerable<string>? bannedWords)
        {
            var normalized = (bannedWords ?? Enumerable.Empty<string>())
                .Select(Normalize)
                .Where(w => w.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var tokens = normalized
                .Where(w => !w.Contains(' ', StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);

            var phrases = normalized
                .Where(w => w.Contains(' ', StringComparison.Ordinal))
                .ToList();

            return new ContentModerationState(enabled, tokens, phrases);
        }

        public static string Normalize(string input)
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
                if (ZeroWidthChars.Contains(ch))
                    continue;
                sb.Append(ch);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
