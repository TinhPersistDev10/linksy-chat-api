using System;
using System.Collections.Generic;
using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Models;

public partial class ChatroomMember
{
    public Guid MemberId { get; set; }

    public Guid ChatroomId { get; set; }

    public Guid UserId { get; set; }

    public string MemberRole { get; set; } = null!;

    public string? Nickname { get; set; }
    public bool? IsMuted { get; set; }
    public DateTime? MutedUntil { get; set; }
    public string? NotificationPreference { get; set; }
    public DateTime? LastReadAt { get; set; }
    public int? MessageCount { get; set; }


    public DateTime? JoinedAt { get; set; }
    public Guid? AddedBy { get; set; }
    public DateTime? LeftAt { get; set; }
    public Guid? RemovedBy { get; set; }
    public virtual Chatroom Chatroom { get; set; } = null!;

    public virtual User User { get; set; } = null!;
    public virtual User? AddedByNavigation { get; set; } = null!;
    public virtual User? RemovedByNavigation { get; set; } = null!;
    public virtual MemberPermission? MemberPermission { get; set; }
}
