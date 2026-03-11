using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace linksy_backend_api.Models;

public partial class AccessToken
{
    public int TokenId { get; set; }

    public Guid UserId { get; set; }

    [Column(TypeName = "text")]
    public string Token { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? RefreshToken { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
