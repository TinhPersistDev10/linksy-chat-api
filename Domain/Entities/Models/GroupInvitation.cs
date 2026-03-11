using System;
using System.Collections.Generic;

namespace linksy_backend_api.Models;

public partial class GroupInvitation
{
    public Guid InvitationId { get; set; }

    public Guid ChatroomId { get; set; }

    public Guid InvitedUserId { get; set; }

    public Guid InvitedBy { get; set; }

    public string Status { get; set; } = null!;

    public string? Message { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public virtual Chatroom Chatroom { get; set; } = null!;

    public virtual User InvitedByNavigation { get; set; } = null!;

    public virtual User InvitedUser { get; set; } = null!;
}
