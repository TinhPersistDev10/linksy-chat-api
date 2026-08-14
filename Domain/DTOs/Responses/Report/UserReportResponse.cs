using System;

namespace linksy_backend_api.DTOs.Report;

public class UserReportResponse
{
    public Guid ReportId { get; set; }
    public Guid ReporterUserId { get; set; }
    public string ReporterUsername { get; set; } = string.Empty;
    public string? ReporterFullname { get; set; }
    public string? ReporterAvatar { get; set; }
    public Guid ReportedUserId { get; set; }
    public string ReportedUsername { get; set; } = string.Empty;
    public string? ReportedFullname { get; set; }
    public string? ReportedAvatar { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? ReviewedByAdminUsername { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? TotalCount { get; set; }
    public int? CurrentPage { get; set; }
    public int? TotalPages { get; set; }

    public string ReportedUserModerationLevel { get; set; } = "none";
    public bool ReportedUserIsFlagged { get; set; }
    public int ReportedUserViolationPoints { get; set; }
}
