using System;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models;

/// <summary>
/// User-submitted account report (Zalo/Messenger-style).
/// CHECK (reporter_user_id != reported_user_id)
/// </summary>
public class UserReport
{
    public Guid ReportId { get; set; }

    public Guid ReporterUserId { get; set; }

    public Guid ReportedUserId { get; set; }

    /// <summary>spam | harassment | fake_account | inappropriate | scam | violence | other</summary>
    public string Reason { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>pending | reviewing | resolved | dismissed</summary>
    public string Status { get; set; } = "pending";

    public string? AdminNote { get; set; }

    public Guid? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User ReporterUser { get; set; } = null!;

    public virtual User ReportedUser { get; set; } = null!;

    public virtual User? ReviewedByAdmin { get; set; }
}
