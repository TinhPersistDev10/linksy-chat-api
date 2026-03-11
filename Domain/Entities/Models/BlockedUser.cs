using System;
using System.Collections.Generic;

namespace linksy_backend_api.Models;

/// <summary>
/// CHECK (blocker_user_id != blocked_user_id)
/// </summary>
public partial class BlockedUser
{
    public Guid BlockId { get; set; }

    public Guid BlockerUserId { get; set; }

    public Guid BlockedUserId { get; set; }

    public DateTime? BlockedAt { get; set; }

    public string? Reason { get; set; }

    public virtual User BlockedUserNavigation { get; set; } = null!;

    public virtual User BlockerUser { get; set; } = null!;
}
