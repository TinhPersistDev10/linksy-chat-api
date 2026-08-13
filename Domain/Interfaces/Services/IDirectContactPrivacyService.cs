namespace linksy_backend_api.Domain.Interfaces.Services;

/// <summary>
/// Enforces privacy for 1-1 contact (messages / calls) based on friendship
/// and the recipient's WhoCanMessageMe setting.
/// </summary>
public interface IDirectContactPrivacyService
{
    /// <param name="action">"message" or "call"</param>
    Task EnsureCanContactUserAsync(Guid actorId, Guid targetUserId, string action);

    /// <summary>
    /// No-op for group chatrooms. For direct chats, checks the other member.
    /// </summary>
    Task EnsureCanContactInChatroomAsync(Guid actorId, Guid chatroomId, string action);
}
