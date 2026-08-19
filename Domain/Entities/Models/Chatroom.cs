using System;
using System.Collections.Generic;
using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Models;

public partial class Chatroom
{
    public Guid ChatroomId { get; set; }

    public string? RoomName { get; set; }

    public string? Description { get; set; }

    public string? Avatar { get; set; }

    /// <summary>Quick-send emoji shown next to the composer when it's empty (default 👍).</summary>
    public string? QuickEmoji { get; set; }

    public string RoomType { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsArchived { get; set; }

    public Guid? LastMessageId { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Message? LastMessage { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<ChatroomMember> ChatroomMembers { get; set; } = new List<ChatroomMember>();

    public virtual ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();
}
