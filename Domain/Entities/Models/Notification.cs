using System;
using System.Collections.Generic;

namespace linksy_backend_api.Models;

public partial class Notification
{
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string? Title { get; set; }

    public string? Body { get; set; }

    public Guid? RelatedEntityId { get; set; }

    public string? RelatedEntityType { get; set; }

    public string? ActionUrl { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsRead { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
