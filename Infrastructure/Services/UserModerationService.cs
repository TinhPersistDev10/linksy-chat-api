using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Moderation;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services;

public class UserModerationService : IUserModerationService
{
    public const int ReportVolumeFlagThreshold = 3;
    public const int ReportVolumeWindowDays = 7;

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserModerationService> _logger;

    public UserModerationService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<UserModerationService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<bool> RefreshExpiredModerationAsync(User user, bool save = true)
    {
        var level = (user.ModerationLevel ?? ModerationLevels.None).ToLowerInvariant();
        if (level is not (ModerationLevels.Restricted or ModerationLevels.TemporaryLock))
            return false;

        if (!user.ModerationExpiresAt.HasValue || user.ModerationExpiresAt > DateTime.UtcNow)
            return false;

        user.ModerationLevel = ModerationLevels.None;
        user.ModerationReason = null;
        user.ModerationExpiresAt = null;
        user.ModeratedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        if (save)
            await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public string GetEffectiveLevel(User user)
    {
        var level = (user.ModerationLevel ?? ModerationLevels.None).ToLowerInvariant();
        if (level is ModerationLevels.Restricted or ModerationLevels.TemporaryLock)
        {
            if (user.ModerationExpiresAt.HasValue && user.ModerationExpiresAt <= DateTime.UtcNow)
                return ModerationLevels.None;
        }
        return level;
    }

    public bool CanLogin(User user)
    {
        if (user.IsActive != true) return false;
        return !ModerationLevels.BlocksLogin(GetEffectiveLevel(user));
    }

    public bool CanSendMessages(User user) =>
        !ModerationLevels.BlocksMessaging(GetEffectiveLevel(user));

    public bool CanStartCalls(User user) =>
        !ModerationLevels.BlocksMessaging(GetEffectiveLevel(user));

    public UserModerationStatusDto ToStatusDto(User user)
    {
        var level = GetEffectiveLevel(user);
        var canMsg = !ModerationLevels.BlocksMessaging(level);
        return new UserModerationStatusDto
        {
            Level = level,
            LevelLabel = ModerationLevels.DisplayNameVi(level),
            Reason = user.ModerationReason,
            ExpiresAt = level is ModerationLevels.Restricted or ModerationLevels.TemporaryLock
                ? user.ModerationExpiresAt
                : null,
            ModeratedAt = user.ModeratedAt,
            ViolationPoints = user.ViolationPoints,
            IsFlaggedForReview = user.IsFlaggedForReview,
            CanLogin = CanLogin(user),
            CanSendMessages = canMsg,
            CanStartCalls = canMsg
        };
    }

    public async Task<ApiResponseDto> ApplyAsync(
        Guid adminUserId,
        Guid targetUserId,
        ApplyModerationRequest request)
    {
        if (adminUserId == targetUserId)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Không thể áp dụng moderation lên chính mình",
                Data = ""
            };
        }

        var target = await _unitOfWork.Users.GetByIdAsync(targetUserId);
        if (target == null)
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Không tìm thấy người dùng",
                Data = ""
            };
        }

        var level = (request.Level ?? string.Empty).Trim().ToLowerInvariant();
        if (!ModerationLevels.All.Contains(level))
        {
            return new ApiResponseDto
            {
                Success = false,
                Message = "Mức moderation không hợp lệ",
                Data = ""
            };
        }

        await ApplyInternalAsync(
            adminUserId,
            target,
            level,
            request.Reason,
            request.DurationDays,
            request.IncrementStrike && level != ModerationLevels.None,
            saveChanges: true);

        return new ApiResponseDto
        {
            Success = true,
            Message = level == ModerationLevels.None
                ? "Đã gỡ hạn chế tài khoản"
                : $"Đã áp dụng: {ModerationLevels.DisplayNameVi(level)}",
            Data = ToStatusDto(target)
        };
    }

    public async Task ApplyInternalAsync(
        Guid adminUserId,
        User target,
        string level,
        string? reason,
        int? durationDays,
        bool incrementStrike,
        bool saveChanges = true)
    {
        level = level.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        if (level == ModerationLevels.None)
        {
            target.ModerationLevel = ModerationLevels.None;
            target.ModerationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            target.ModerationExpiresAt = null;
            target.ModeratedAt = now;
            target.ModeratedByAdminId = adminUserId;
            target.IsActive = true;
            target.IsFlaggedForReview = false;
            target.UpdatedAt = now;
        }
        else
        {
            if (!ModerationLevels.Actionable.Contains(level))
                throw new ArgumentException("Invalid moderation level");

            var days = durationDays
                ?? ModerationLevels.DefaultDurationDays(level);
            DateTime? expires = level is ModerationLevels.Restricted or ModerationLevels.TemporaryLock
                ? now.AddDays(Math.Clamp(days, 1, 365))
                : null;

            target.ModerationLevel = level;
            target.ModerationReason = string.IsNullOrWhiteSpace(reason)
                ? ModerationLevels.DisplayNameVi(level)
                : reason.Trim();
            target.ModerationExpiresAt = expires;
            target.ModeratedAt = now;
            target.ModeratedByAdminId = adminUserId;
            target.UpdatedAt = now;

            if (incrementStrike)
                target.ViolationPoints += ModerationLevels.StrikePoints(level);

            // Login-blocking levels also deactivate for existing IsActive checks
            if (ModerationLevels.BlocksLogin(level))
            {
                target.IsActive = false;
                await _unitOfWork.TokenRepository.RevokeAllByUserIdAsync(target.UserId);
            }
            else
            {
                target.IsActive = true;
            }

            // Serious actions clear review flag after admin handled it
            if (level is ModerationLevels.TemporaryLock or ModerationLevels.PermanentLock)
                target.IsFlaggedForReview = false;
        }

        _unitOfWork.Users.Update(target);

        if (saveChanges)
            await _unitOfWork.SaveChangesAsync();

        try
        {
            await NotifyUserAsync(target, level);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify user {UserId} about moderation", target.UserId);
        }
    }

    public async Task EvaluateReportVolumeFlagAsync(Guid reportedUserId)
    {
        var since = DateTime.UtcNow.AddDays(-ReportVolumeWindowDays);
        var pendingCount = await _unitOfWork.UserReports.Query()
            .CountAsync(r =>
                r.ReportedUserId == reportedUserId &&
                r.Status == "pending" &&
                r.CreatedAt >= since);

        if (pendingCount < ReportVolumeFlagThreshold)
            return;

        var user = await _unitOfWork.Users.GetByIdAsync(reportedUserId);
        if (user == null || user.IsFlaggedForReview)
            return;

        // Do not escalate lock automatically — only flag for admin priority
        user.IsFlaggedForReview = true;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} flagged for review after {Count} pending reports in {Days} days",
            reportedUserId, pendingCount, ReportVolumeWindowDays);
    }

    private async Task NotifyUserAsync(User target, string level)
    {
        string title;
        string body;

        switch (level)
        {
            case ModerationLevels.None:
                title = "Hạn chế tài khoản đã được gỡ";
                body = "Tài khoản của bạn đã được khôi phục quyền sử dụng bình thường.";
                break;
            case ModerationLevels.Warning:
                title = "Cảnh báo vi phạm";
                body = string.IsNullOrWhiteSpace(target.ModerationReason)
                    ? "Tài khoản của bạn đã nhận cảnh báo do vi phạm tiêu chuẩn cộng đồng."
                    : $"Cảnh báo: {target.ModerationReason}";
                break;
            case ModerationLevels.Restricted:
                title = "Tài khoản bị hạn chế";
                body = BuildExpiryBody(
                    "Bạn tạm thời không thể nhắn tin hoặc gọi.",
                    target.ModerationExpiresAt);
                break;
            case ModerationLevels.TemporaryLock:
                title = "Tài khoản bị khóa tạm thời";
                body = BuildExpiryBody(
                    "Bạn không thể đăng nhập trong thời gian khóa.",
                    target.ModerationExpiresAt);
                break;
            case ModerationLevels.PermanentLock:
                title = "Tài khoản bị khóa vĩnh viễn";
                body = string.IsNullOrWhiteSpace(target.ModerationReason)
                    ? "Tài khoản của bạn đã bị vô hiệu hóa do vi phạm nghiêm trọng."
                    : target.ModerationReason;
                break;
            default:
                return;
        }

        await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
        {
            UserId = target.UserId,
            NotificationType = "moderation",
            Title = title,
            Body = body.Length > 500 ? body[..500] : body,
            RelatedEntityType = "user",
            RelatedEntityId = target.UserId,
            ActionUrl = "/settings"
        });
    }

    private static string BuildExpiryBody(string prefix, DateTime? expiresAt)
    {
        if (!expiresAt.HasValue) return prefix;
        var local = expiresAt.Value.ToString("dd/MM/yyyy HH:mm") + " UTC";
        return $"{prefix} Hết hạn khoảng: {local}.";
    }
}
