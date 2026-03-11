using System;
using System.Collections.Generic;

namespace linksy_backend_api.Models;

public partial class EmailOtp
{
    public long Id { get; set; }

    public Guid? UserId { get; set; }

    public string Email { get; set; } = null!;

    public string OtpCode { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public int? Attempts { get; set; }

    public int? MaxAttempts { get; set; }

    public bool? IsUsed { get; set; }

    public bool? IsExpired { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public virtual User? User { get; set; }
}
