using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Moderation;
using linksy_backend_api.Models;

namespace linksy_backend_api.Core.Interfaces.Services;

public interface IUserModerationService
{
    /// <summary>Expire temporary moderation if past ModerationExpiresAt.</summary>
    Task<bool> RefreshExpiredModerationAsync(User user, bool save = true);

    string GetEffectiveLevel(User user);

    bool CanLogin(User user);

    bool CanSendMessages(User user);

    bool CanStartCalls(User user);

    UserModerationStatusDto ToStatusDto(User user);

    Task<ApiResponseDto> ApplyAsync(Guid adminUserId, Guid targetUserId, ApplyModerationRequest request);

    Task ApplyInternalAsync(
        Guid adminUserId,
        User target,
        string level,
        string? reason,
        int? durationDays,
        bool incrementStrike,
        bool saveChanges = true);

    /// <summary>Flag account for review when many pending reports arrive (no auto-lock).</summary>
    Task EvaluateReportVolumeFlagAsync(Guid reportedUserId);
}
