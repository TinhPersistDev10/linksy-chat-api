using System;
using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.DTOs.Report;

public class CreateUserReportRequest
{
    [Required]
    public Guid ReportedUserId { get; set; }

    /// <summary>spam | harassment | fake_account | inappropriate | scam | violence | other</summary>
    [Required]
    [StringLength(50)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>Also block the reported user after submitting (Messenger-style).</summary>
    public bool AlsoBlock { get; set; }
}
