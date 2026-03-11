using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace linksy_backend_api.Models;

public partial class FriendRequest
{
    public Guid RequestId { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Status { get; set; } = "pending"; // pending, accepted, rejected, cancelled
    public string? Message { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SenderId))]
    public virtual User Sender { get; set; } = null!;
    [ForeignKey(nameof(ReceiverId))]
    public virtual User Receiver { get; set; } = null!;
}
