namespace linksy_backend_api.Core.DTOs.Responses.Friends;

public class FriendInviteLinkResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class FriendInvitePreviewResponse
{
    public Guid UserId { get; set; }
    public Guid InviterId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsUsed { get; set; }
}

public class AcceptFriendInviteResponse
{
    public string Status { get; set; } = string.Empty;
    public Guid InviterId { get; set; }
}
