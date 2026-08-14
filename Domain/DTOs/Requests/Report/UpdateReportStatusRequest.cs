using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.DTOs.Report;

public class UpdateReportStatusRequest
{
    /// <summary>reviewing | resolved | dismissed</summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? AdminNote { get; set; }

    /// <summary>
    /// When resolving: none | warning | restricted | temporary_lock | permanent_lock.
    /// Replaces blunt deactivate flag.
    /// </summary>
    [StringLength(30)]
    public string? ModerationAction { get; set; }

    /// <summary>Duration for restricted / temporary_lock (days).</summary>
    [Range(1, 365)]
    public int? DurationDays { get; set; }

    public bool IncrementStrike { get; set; } = true;

    /// <summary>Legacy: treat as permanent_lock when resolving.</summary>
    public bool DeactivateReportedUser { get; set; }
}
