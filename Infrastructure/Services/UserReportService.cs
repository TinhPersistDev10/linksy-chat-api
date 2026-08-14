using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Block;
using linksy_backend_api.DTOs.Report;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services;

public class UserReportService : IUserReportService
{
    private static readonly HashSet<string> AllowedReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "spam",
        "harassment",
        "fake_account",
        "inappropriate",
        "scam",
        "violence",
        "other"
    };

    private static readonly HashSet<string> AllowedAdminStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "reviewing",
        "resolved",
        "dismissed"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlockedService _blockedService;
    private readonly IUserModerationService _moderationService;
    private readonly ILogger<UserReportService> _logger;

    public UserReportService(
        IUnitOfWork unitOfWork,
        IBlockedService blockedService,
        IUserModerationService moderationService,
        ILogger<UserReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _blockedService = blockedService;
        _moderationService = moderationService;
        _logger = logger;
    }

    public IReadOnlyList<object> GetReportReasons() => new List<object>
    {
        new { code = "spam", label = "Spam hoặc quảng cáo" },
        new { code = "harassment", label = "Quấy rối hoặc bắt nạt" },
        new { code = "fake_account", label = "Tài khoản giả mạo" },
        new { code = "inappropriate", label = "Nội dung không phù hợp" },
        new { code = "scam", label = "Lừa đảo / gian lận" },
        new { code = "violence", label = "Bạo lực hoặc đe dọa" },
        new { code = "other", label = "Khác" }
    };

    public async Task<ApiResponseDto> CreateReportAsync(Guid reporterUserId, CreateUserReportRequest request)
    {
        try
        {
            if (reporterUserId == request.ReportedUserId)
            {
                return Fail("Không thể báo cáo chính mình");
            }

            var reason = (request.Reason ?? string.Empty).Trim().ToLowerInvariant();
            if (!AllowedReasons.Contains(reason))
            {
                return Fail("Lý do báo cáo không hợp lệ");
            }

            var reportedUser = await _unitOfWork.Users.GetByIdAsync(request.ReportedUserId);
            if (reportedUser == null)
            {
                return Fail("Không tìm thấy người dùng bị báo cáo");
            }

            var pendingExists = await _unitOfWork.UserReports.Query()
                .AnyAsync(r =>
                    r.ReporterUserId == reporterUserId &&
                    r.ReportedUserId == request.ReportedUserId &&
                    r.Status == "pending");

            if (pendingExists)
            {
                return Fail("Bạn đã gửi báo cáo cho tài khoản này và đang chờ xử lý");
            }

            var report = new UserReport
            {
                ReportId = Guid.NewGuid(),
                ReporterUserId = reporterUserId,
                ReportedUserId = request.ReportedUserId,
                Reason = reason,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.UserReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            await _moderationService.EvaluateReportVolumeFlagAsync(request.ReportedUserId);

            if (request.AlsoBlock)
            {
                try
                {
                    await _blockedService.BlockUserAsync(reporterUserId, new BlockUserRequest
                    {
                        BlockedUserId = request.ReportedUserId,
                        Reason = $"Báo cáo: {reason}"
                    });
                }
                catch (InvalidOperationException)
                {
                    // Already blocked — ignore
                }
            }

            var dto = await MapReportAsync(report.ReportId);

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đã gửi báo cáo. Cảm ơn bạn đã giúp giữ cộng đồng an toàn.",
                Data = dto!
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user report");
            return Fail("Không thể gửi báo cáo");
        }
    }

    public async Task<ApiResponseDto> GetMyReportsAsync(Guid reporterUserId, int page, int pageSize)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _unitOfWork.UserReports.Query()
                .Where(r => r.ReporterUserId == reporterUserId);

            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

            var ids = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => r.ReportId)
                .ToListAsync();

            var items = new List<UserReportResponse>();
            foreach (var id in ids)
            {
                var mapped = await MapReportAsync(id);
                if (mapped == null) continue;
                mapped.TotalCount = total;
                mapped.CurrentPage = page;
                mapped.TotalPages = totalPages;
                items.Add(mapped);
            }

            return new ApiResponseDto
            {
                Success = true,
                Message = "OK",
                Data = items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing my reports");
            return Fail("Không thể tải danh sách báo cáo");
        }
    }

    public async Task<ApiResponseDto> GetReportsForAdminAsync(string? status, int page, int pageSize)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _unitOfWork.UserReports.Query();
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalized = status.Trim().ToLowerInvariant();
                query = query.Where(r => r.Status == normalized);
            }

            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

            var ids = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => r.ReportId)
                .ToListAsync();

            var items = new List<UserReportResponse>();
            foreach (var id in ids)
            {
                var mapped = await MapReportAsync(id);
                if (mapped == null) continue;
                mapped.TotalCount = total;
                mapped.CurrentPage = page;
                mapped.TotalPages = totalPages;
                items.Add(mapped);
            }

            return new ApiResponseDto
            {
                Success = true,
                Message = "OK",
                Data = items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing reports for admin");
            return Fail("Không thể tải danh sách báo cáo");
        }
    }

    public async Task<ApiResponseDto> GetReportByIdAsync(Guid reportId)
    {
        try
        {
            var dto = await MapReportAsync(reportId);
            if (dto == null)
            {
                return Fail("Không tìm thấy báo cáo");
            }

            return new ApiResponseDto
            {
                Success = true,
                Message = "OK",
                Data = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", reportId);
            return Fail("Không thể tải báo cáo");
        }
    }

    public async Task<ApiResponseDto> UpdateReportStatusAsync(
        Guid adminUserId,
        Guid reportId,
        UpdateReportStatusRequest request)
    {
        try
        {
            var status = (request.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (!AllowedAdminStatuses.Contains(status))
            {
                return Fail("Trạng thái không hợp lệ (reviewing | resolved | dismissed)");
            }

            var report = await _unitOfWork.UserReports.GetByIdAsync(reportId);
            if (report == null)
            {
                return Fail("Không tìm thấy báo cáo");
            }

            report.Status = status;
            report.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote)
                ? report.AdminNote
                : request.AdminNote.Trim();
            report.ReviewedByAdminId = adminUserId;
            report.ReviewedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.UserReports.Update(report);
            await _unitOfWork.SaveChangesAsync();

            if (status == "resolved" && report.ReportedUserId != adminUserId)
            {
                var action = ResolveModerationAction(request);
                if (!string.IsNullOrEmpty(action) && action != ModerationLevels.None)
                {
                    var reported = await _unitOfWork.Users.GetByIdAsync(report.ReportedUserId);
                    if (reported != null)
                    {
                        var reason = string.IsNullOrWhiteSpace(request.AdminNote)
                            ? $"Xử lý báo cáo: {report.Reason}"
                            : request.AdminNote.Trim();

                        await _moderationService.ApplyInternalAsync(
                            adminUserId,
                            reported,
                            action,
                            reason,
                            request.DurationDays,
                            request.IncrementStrike,
                            saveChanges: true);
                    }
                }
            }

            if (status is "resolved" or "dismissed")
            {
                var reported = await _unitOfWork.Users.GetByIdAsync(report.ReportedUserId);
                if (reported is { IsFlaggedForReview: true })
                {
                    var stillPending = await _unitOfWork.UserReports.Query()
                        .AnyAsync(r =>
                            r.ReportedUserId == report.ReportedUserId &&
                            r.Status == "pending" &&
                            r.ReportId != report.ReportId);
                    if (!stillPending)
                    {
                        reported.IsFlaggedForReview = false;
                        reported.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Users.Update(reported);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
            }

            var dto = await MapReportAsync(reportId);
            return new ApiResponseDto
            {
                Success = true,
                Message = "Đã cập nhật báo cáo",
                Data = dto!
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report {ReportId}", reportId);
            return Fail("Không thể cập nhật báo cáo");
        }
    }

    private static string ResolveModerationAction(UpdateReportStatusRequest request)
    {
        if (request.DeactivateReportedUser)
            return ModerationLevels.PermanentLock;

        var action = (request.ModerationAction ?? ModerationLevels.None).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(action))
            return ModerationLevels.None;
        if (!ModerationLevels.All.Contains(action))
            throw new InvalidOperationException("ModerationAction không hợp lệ");
        return action;
    }

    private async Task<UserReportResponse?> MapReportAsync(Guid reportId)
    {
        var report = await _unitOfWork.UserReports.Query()
            .Include(r => r.ReporterUser)
            .Include(r => r.ReportedUser)
            .Include(r => r.ReviewedByAdmin)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null) return null;

        var reported = report.ReportedUser;
        var level = reported != null
            ? _moderationService.GetEffectiveLevel(reported)
            : ModerationLevels.None;

        return new UserReportResponse
        {
            ReportId = report.ReportId,
            ReporterUserId = report.ReporterUserId,
            ReporterUsername = report.ReporterUser?.Username ?? string.Empty,
            ReporterFullname = report.ReporterUser?.Fullname,
            ReporterAvatar = report.ReporterUser?.Avatar,
            ReportedUserId = report.ReportedUserId,
            ReportedUsername = report.ReportedUser?.Username ?? string.Empty,
            ReportedFullname = report.ReportedUser?.Fullname,
            ReportedAvatar = report.ReportedUser?.Avatar,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status,
            AdminNote = report.AdminNote,
            ReviewedByAdminId = report.ReviewedByAdminId,
            ReviewedByAdminUsername = report.ReviewedByAdmin?.Username,
            ReviewedAt = report.ReviewedAt,
            CreatedAt = report.CreatedAt,
            ReportedUserModerationLevel = level,
            ReportedUserIsFlagged = reported?.IsFlaggedForReview ?? false,
            ReportedUserViolationPoints = reported?.ViolationPoints ?? 0
        };
    }

    private static ApiResponseDto Fail(string message) => new()
    {
        Success = false,
        Message = message,
        Data = ""
    };
}
