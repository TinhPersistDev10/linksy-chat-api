using System;
using System.Collections.Generic;

namespace linksy_backend_api.Models;

public partial class Message
{
    public Guid MessageId { get; set; }

    public Guid ChatroomId { get; set; }

    public Guid? SenderId { get; set; }

    public string MessageType { get; set; } = null!;

    public string? MessageText { get; set; }

    public Guid? ParentMessageId { get; set; }

    public int? ReplyCount { get; set; }

    public bool? IsEdited { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Chatroom Chatroom { get; set; } = null!;

    public virtual ICollection<Chatroom> Chatrooms { get; set; } = new List<Chatroom>();

    public virtual ICollection<Message> InverseParentMessage { get; set; } = new List<Message>();

    public virtual Message? ParentMessage { get; set; }

    public virtual User? Sender { get; set; }
}
