using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.DTOs.Moderation;

public class ApplyModerationRequest
{
    /// <summary>warning | restricted | temporary_lock | permanent_lock | none (lift)</summary>
    [Required]
    [StringLength(30)]
    public string Level { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>Days until expiry for restricted / temporary_lock. Ignored for warning/permanent/none.</summary>
    [Range(1, 365)]
    public int? DurationDays { get; set; }

    public bool IncrementStrike { get; set; } = true;
}
