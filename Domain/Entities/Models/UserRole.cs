using System;
using System.Collections.Generic;

namespace linksy_backend_api.Models;

public partial class UserRole
{
    public int UserRoleId { get; set; }

    public Guid UserId { get; set; }

    public int RoleId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
