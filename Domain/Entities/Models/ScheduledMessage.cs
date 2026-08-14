using System;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models;

public class ScheduledMessage
{
    public Guid Id { get; set; }

    public Guid ChatroomId { get; set; }

    public Guid SenderId { get; set; }

    public string MessageType { get; set; } = "text";

    public string MessageText { get; set; } = string.Empty;

    public Guid? ParentMessageId { get; set; }

    public DateTime SendAt { get; set; }

    /// <summary>pending | sent | cancelled</summary>
    public string Status { get; set; } = "pending";

    public DateTime CreatedAt { get; set; }

    public virtual Chatroom Chatroom { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;

    public virtual Message? ParentMessage { get; set; }
}
