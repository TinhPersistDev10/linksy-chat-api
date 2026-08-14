using System.ComponentModel.DataAnnotations.Schema;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models;

public class FriendInvite
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid InviterId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(InviterId))]
    public virtual User Inviter { get; set; } = null!;
}
