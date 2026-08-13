namespace linksy_backend_api.Domain.Enums;

/// <summary>
/// Hybrid moderation levels (Messenger-style severity, admin-reviewed like Zalo).
/// </summary>
public static class ModerationLevels
{
    public const string None = "none";
    public const string Warning = "warning";
    public const string Restricted = "restricted";
    public const string TemporaryLock = "temporary_lock";
    public const string PermanentLock = "permanent_lock";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        None, Warning, Restricted, TemporaryLock, PermanentLock
    };

    public static readonly HashSet<string> Actionable = new(StringComparer.OrdinalIgnoreCase)
    {
        Warning, Restricted, TemporaryLock, PermanentLock
    };

    public static bool BlocksLogin(string? level) =>
        string.Equals(level, TemporaryLock, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(level, PermanentLock, StringComparison.OrdinalIgnoreCase);

    public static bool BlocksMessaging(string? level) =>
        string.Equals(level, Restricted, StringComparison.OrdinalIgnoreCase) ||
        BlocksLogin(level);

    public static int StrikePoints(string level) => level.ToLowerInvariant() switch
    {
        Warning => 1,
        Restricted => 2,
        TemporaryLock => 3,
        PermanentLock => 5,
        _ => 0
    };

    public static int DefaultDurationDays(string level) => level.ToLowerInvariant() switch
    {
        Restricted => 7,
        TemporaryLock => 30,
        _ => 0
    };

    public static string DisplayNameVi(string? level) => (level ?? None).ToLowerInvariant() switch
    {
        Warning => "Cảnh báo",
        Restricted => "Hạn chế tính năng",
        TemporaryLock => "Khóa tạm thời",
        PermanentLock => "Khóa vĩnh viễn",
        _ => "Không"
    };
}
