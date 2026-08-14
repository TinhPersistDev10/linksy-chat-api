using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services;

public sealed class DirectContactPrivacyService : IDirectContactPrivacyService
{
    private readonly IUnitOfWork _unitOfWork;

    public DirectContactPrivacyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task EnsureCanContactUserAsync(Guid actorId, Guid targetUserId, string action)
    {
        if (actorId == targetUserId) return;

        var isCall = action.Equals("call", StringComparison.OrdinalIgnoreCase);
        await EnsureNotBlockedAsync(actorId, targetUserId, isCall);

        var areFriends = await _unitOfWork.FriendshipRepository.AreFriendsAsync(actorId, targetUserId);
        if (areFriends) return;

        var privacy = await _unitOfWork.PrivacySettingsRepository.GetOrCreateAsync(targetUserId);
        // Persist defaults if newly created (safe no-op when already tracked)
        await _unitOfWork.SaveChangesAsync();

        var whoCan = (privacy.WhoCanMessageMe ?? "everyone").Trim().ToLowerInvariant();
        if (whoCan != "friends") return;

        var target = await _unitOfWork.Users.GetByIdAsync(targetUserId);
        var displayName = target?.Fullname ?? target?.Username ?? "Người dùng này";

        var noun = isCall
            ? "cuộc gọi từ người lạ"
            : "tin nhắn của người lạ";

        throw new InvalidOperationException($"{displayName} không nhận {noun}");
    }

    private async Task EnsureNotBlockedAsync(Guid actorId, Guid targetUserId, bool isCall)
    {
        var actorBlockedTarget = await _unitOfWork.BlockedUserRepository
            .IsBlockedAsync(actorId, targetUserId);
        if (actorBlockedTarget)
        {
            var target = await _unitOfWork.Users.GetByIdAsync(targetUserId);
            var displayName = target?.Fullname ?? target?.Username ?? "người dùng này";
            throw new InvalidOperationException(
                isCall
                    ? $"Bạn đã chặn {displayName}. Hãy bỏ chặn để gọi."
                    : $"Bạn đã chặn {displayName}. Hãy bỏ chặn để nhắn tin.");
        }

        var targetBlockedActor = await _unitOfWork.BlockedUserRepository
            .IsBlockedAsync(targetUserId, actorId);
        if (targetBlockedActor)
        {
            throw new InvalidOperationException(
                isCall
                    ? "Không thể gọi. Người dùng này đã chặn bạn."
                    : "Không thể gửi tin nhắn. Người dùng này đã chặn bạn.");
        }
    }

    public async Task EnsureCanContactInChatroomAsync(Guid actorId, Guid chatroomId, string action)
    {
        var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
        if (chatroom is null) return;
        if (!string.Equals(chatroom.RoomType, "direct", StringComparison.OrdinalIgnoreCase))
            return;

        var otherUserId = await _unitOfWork.ChatroomMembers.Query()
            .Where(m => m.ChatroomId == chatroomId && m.UserId != actorId && m.LeftAt == null)
            .Select(m => m.UserId)
            .FirstOrDefaultAsync();

        if (otherUserId == Guid.Empty) return;

        await EnsureCanContactUserAsync(actorId, otherUserId, action);
    }
}
