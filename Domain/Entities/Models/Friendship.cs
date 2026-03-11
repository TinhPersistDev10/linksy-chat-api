using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace linksy_backend_api.Models;

/// <summary>
/// CHECK (user1_id != user2_id AND user1_id &lt; user2_id)
/// </summary>
public partial class Friendship
{
    public Guid FriendshipId { get; set; }

    public Guid User1Id { get; set; }

    public Guid User2Id { get; set; }

    public DateTime? CreatedAt { get; set; }
    [ForeignKey(nameof(User1Id))]
    public virtual User User1 { get; set; } = null!;

    [ForeignKey(nameof(User2Id))]
    public virtual User User2 { get; set; } = null!;
}
