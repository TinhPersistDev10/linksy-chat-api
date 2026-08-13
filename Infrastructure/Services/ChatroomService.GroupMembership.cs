using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.ChatroomDTO;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Infrastructure.Mappers;
using linksy_backend_api.Models;
using Microsoft.AspNetCore.SignalR;

namespace linksy_backend_api.Services;

public partial class ChatroomService
{
    public async Task<ApiResponseDto> AddMembersAsync(
        Guid userId,
        Guid chatroomId,
        AddMembersRequest request)
    {
        var chatroom = await RequireGroupAsync(chatroomId);
        var caller = await _unitOfWork.ChatroomMemberRepository
            .GetActiveMemberAsync(chatroomId, userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this group.");

        var canInvite = caller.MemberRole == "admin" ||
            await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(
                userId, chatroomId, PermissionType.CanInviteMembers);
        if (!canInvite)
            throw new UnauthorizedAccessException("You do not have permission to add members.");

        var actor = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Acting user does not exist.");
        var requestedIds = request.MemberIds
            .Where(id => id != userId)
            .Distinct()
            .ToList();
        var validUserIds = await _unitOfWork.UserRepository
            .GetExistingUserIdsAsync(requestedIds);
        var addedUsers = new List<User>();
        var systemMessages = new List<Message>();

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var memberUserId in validUserIds)
            {
                var membership = await _unitOfWork.ChatroomMembers.FirstOrDefaultAsync(
                    member => member.ChatroomId == chatroomId &&
                              member.UserId == memberUserId);

                if (membership is not null && membership.LeftAt is null)
                    continue;

                if (membership is null)
                {
                    membership = CreateMember(chatroomId, memberUserId, "member", userId);
                    await _unitOfWork.ChatroomMembers.AddAsync(membership);
                    await _unitOfWork.MemberPermissionRepository
                        .CreateDefaultAsync(membership.MemberId);
                }
                else
                {
                    membership.MemberRole = "member";
                    membership.JoinedAt = DateTime.UtcNow;
                    membership.AddedBy = userId;
                    membership.LeftAt = null;
                    membership.RemovedBy = null;
                    membership.IsMuted = false;
                    membership.MutedUntil = null;
                    membership.NotificationPreference = "all";
                    membership.ClearedAt = null;
                    membership.IsPinned = false;
                    membership.PinnedAt = null;
                    _unitOfWork.ChatroomMembers.Update(membership);
                    await ResetMembershipPermissionsAsync(membership.MemberId);
                }

                var addedUser = await _unitOfWork.Users.GetByIdAsync(memberUserId);
                if (addedUser is null) continue;

                addedUsers.Add(addedUser);
                systemMessages.Add(await AddMembershipSystemMessageAsync(
                    chatroom,
                    userId,
                    $"{DisplayName(actor)} added {DisplayName(addedUser)} to the group."));
            }

            if (addedUsers.Count == 0)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponseDto
                {
                    Success = true,
                    Message = "No new members were added."
                };
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        var activeMemberIds = await _unitOfWork.ChatroomRepository
            .GetActiveMemberIdsAsync(chatroomId);
        await InvalidateGroupCachesAsync(
            chatroomId,
            activeMemberIds.Concat(addedUsers.Select(user => user.UserId)));

        try
        {
            await _notificationService.CreateBulkNotificationsAsync(
                addedUsers.Select(addedUser => new CreateNotificationRequest
                {
                    UserId = addedUser.UserId,
                    NotificationType = "added_to_group",
                    Title = "Added to group",
                    Body = $"{DisplayName(actor)} added you to the group '{chatroom.RoomName ?? "Group"}'.",
                    RelatedEntityId = chatroomId,
                    RelatedEntityType = "chatroom",
                    ActionUrl = $"/dashboard?chatroomId={chatroomId}",
                    ImageUrl = chatroom.Avatar
                }).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create add-member notifications for {ChatroomId}", chatroomId);
        }

        foreach (var message in systemMessages)
            await BroadcastMembershipSystemMessageAsync(message, userId);

        await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync(
            "MembersAdded",
            new
            {
                ChatroomId = chatroomId,
                AddedBy = userId,
                MemberIds = addedUsers.Select(user => user.UserId).ToArray()
            });

        foreach (var addedUser in addedUsers)
        {
            var connections = await _connectionManager
                .GetConnectionsAsync(addedUser.UserId);
            if (connections.Any())
            {
                await _hubContext.Clients.Clients(connections).SendAsync(
                    "AddedToGroup",
                    await MapToChatroomResponseAsync(chatroom, addedUser.UserId));
            }
        }

        return new ApiResponseDto
        {
            Success = true,
            Message = $"Added {addedUsers.Count} member(s)."
        };
    }

    public async Task<ApiResponseDto> RemoveMemberAsync(
        Guid userId,
        Guid chatroomId,
        Guid memberId)
    {
        if (userId == memberId)
            throw new InvalidOperationException("Use the leave-group operation to remove yourself.");

        var chatroom = await RequireGroupAsync(chatroomId);
        var caller = await _unitOfWork.ChatroomMemberRepository
            .GetActiveMemberAsync(chatroomId, userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this group.");
        var canRemove = caller.MemberRole == "admin" ||
            await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(
                userId, chatroomId, PermissionType.CanRemoveMembers);
        if (!canRemove)
            throw new UnauthorizedAccessException("You do not have permission to remove members.");

        var target = await _unitOfWork.ChatroomMemberRepository
            .GetActiveMemberAsync(chatroomId, memberId)
            ?? throw new KeyNotFoundException("The member is not active in this group.");
        if (target.MemberRole == "admin" && chatroom.CreatedBy != userId)
            throw new UnauthorizedAccessException("Only the group creator can remove another admin.");

        var actor = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Acting user does not exist.");
        var removedUser = await _unitOfWork.Users.GetByIdAsync(memberId)
            ?? throw new KeyNotFoundException("Member user does not exist.");
        Message systemMessage;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            target.LeftAt = DateTime.UtcNow;
            target.RemovedBy = userId;
            _unitOfWork.ChatroomMembers.Update(target);
            systemMessage = await AddMembershipSystemMessageAsync(
                chatroom,
                userId,
                $"{DisplayName(actor)} removed {DisplayName(removedUser)} from the group.");
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        var remainingMemberIds = await _unitOfWork.ChatroomRepository
            .GetActiveMemberIdsAsync(chatroomId);
        await InvalidateGroupCachesAsync(
            chatroomId,
            remainingMemberIds.Append(memberId));
        await RemoveUserConnectionsFromGroupAsync(memberId, chatroomId);

        try
        {
            await _notificationService.CreateNotificationAsync(
                new CreateNotificationRequest
                {
                    UserId = memberId,
                    NotificationType = "removed_from_group",
                    Title = "Removed from group",
                    Body = $"You were removed from the group '{chatroom.RoomName ?? "Group"}'.",
                    RelatedEntityId = chatroomId,
                    RelatedEntityType = "chatroom",
                    ImageUrl = chatroom.Avatar
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create removed-member notification for {UserId}", memberId);
        }

        var removedConnections = await _connectionManager.GetConnectionsAsync(memberId);
        if (removedConnections.Any())
            await _hubContext.Clients.Clients(removedConnections)
                .SendAsync("RemovedFromGroup", chatroomId);

        await BroadcastMembershipSystemMessageAsync(systemMessage, userId);
        await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync(
            "MemberRemoved",
            new { ChatroomId = chatroomId, RemovedUserId = memberId, RemovedBy = userId });

        return new ApiResponseDto { Success = true, Message = "Member removed." };
    }

    public async Task<ApiResponseDto> LeaveChatroomAsync(Guid userId, Guid chatroomId)
    {
        var chatroom = await RequireGroupAsync(chatroomId);
        var member = await _unitOfWork.ChatroomMemberRepository
            .GetActiveMemberAsync(chatroomId, userId)
            ?? throw new InvalidOperationException("You are not an active member of this group.");
        var leavingUser = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Leaving user does not exist.");
        ChatroomMember? promotedMember = null;
        Message systemMessage;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (member.MemberRole == "admin" &&
                !await _unitOfWork.ChatroomMemberRepository.HasOtherAdminAsync(chatroomId, userId))
            {
                promotedMember = await _unitOfWork.ChatroomMemberRepository
                    .GetNextMemberToPromoteAsync(chatroomId, userId);
                if (promotedMember is null)
                {
                    chatroom.IsActive = false;
                    chatroom.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Chatrooms.Update(chatroom);
                }
                else
                {
                    promotedMember.MemberRole = "admin";
                    _unitOfWork.ChatroomMembers.Update(promotedMember);
                    await GrantAdminPermissionsAsync(promotedMember.MemberId);
                }
            }

            member.LeftAt = DateTime.UtcNow;
            member.RemovedBy = null;
            _unitOfWork.ChatroomMembers.Update(member);
            systemMessage = await AddMembershipSystemMessageAsync(
                chatroom,
                userId,
                $"{DisplayName(leavingUser)} left the group.");
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        var remainingMemberIds = await _unitOfWork.ChatroomRepository
            .GetActiveMemberIdsAsync(chatroomId);
        await InvalidateGroupCachesAsync(chatroomId, remainingMemberIds.Append(userId));
        await RemoveUserConnectionsFromGroupAsync(userId, chatroomId);

        var leavingConnections = await _connectionManager.GetConnectionsAsync(userId);
        if (leavingConnections.Any())
            await _hubContext.Clients.Clients(leavingConnections)
                .SendAsync("RemovedFromGroup", chatroomId);

        if (promotedMember is not null)
        {
            var promotedConnections = await _connectionManager
                .GetConnectionsAsync(promotedMember.UserId);
            if (promotedConnections.Any())
                await _hubContext.Clients.Clients(promotedConnections)
                    .SendAsync("PromotedToAdmin", new { ChatroomId = chatroomId });

            try
            {
                await _notificationService.CreateNotificationAsync(
                    new CreateNotificationRequest
                    {
                        UserId = promotedMember.UserId,
                        NotificationType = "promoted_to_group_admin",
                        Title = "Promoted to group admin",
                        Body = $"You are now an admin of '{chatroom.RoomName ?? "Group"}'.",
                        RelatedEntityId = chatroomId,
                        RelatedEntityType = "chatroom",
                        ImageUrl = chatroom.Avatar
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create admin-promotion notification for {UserId}", promotedMember.UserId);
            }
        }

        if (chatroom.IsActive != false)
        {
            await BroadcastMembershipSystemMessageAsync(systemMessage, userId);
            await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync(
                "MemberLeft",
                new { ChatroomId = chatroomId, UserId = userId });
        }

        return new ApiResponseDto
        {
            Success = true,
            Message = chatroom.IsActive == false
                ? "Left and disbanded the group."
                : "Left the group."
        };
    }

    private async Task<Chatroom> RequireGroupAsync(Guid chatroomId)
    {
        var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
            ?? throw new KeyNotFoundException("Chatroom does not exist.");
        if (chatroom.RoomType == "direct")
            throw new InvalidOperationException("This operation is only valid for groups.");
        return chatroom;
    }

    private async Task<Message> AddMembershipSystemMessageAsync(
        Chatroom chatroom,
        Guid actorUserId,
        string text)
    {
        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ChatroomId = chatroom.ChatroomId,
            SenderId = actorUserId,
            MessageType = "system",
            MessageText = text,
            SentAt = DateTime.UtcNow,
            IsEdited = false,
            IsDeleted = false
        };
        await _unitOfWork.Messages.AddAsync(message);

        chatroom.LastMessageId = message.MessageId;
        chatroom.LastMessageAt = message.SentAt;
        chatroom.LastActivityAt = message.SentAt;
        chatroom.UpdatedAt = message.SentAt;
        _unitOfWork.Chatrooms.Update(chatroom);
        return message;
    }

    private async Task BroadcastMembershipSystemMessageAsync(Message message, Guid actorUserId)
    {
        var response = await MessageMapper.ToResponseAsync(message, _unitOfWork, actorUserId);
        await _hubContext.Clients.Group(message.ChatroomId.ToString())
            .SendAsync("ReceiveMessage", response);
    }

    private async Task ResetMembershipPermissionsAsync(Guid memberId)
    {
        var permission = await _unitOfWork.MemberPermissionRepository
            .GetByMemberIdAsync(memberId);
        if (permission is null)
        {
            await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(memberId);
            return;
        }

        permission.CanSendMessages = true;
        permission.CanSendMedia = true;
        permission.CanSendVoice = true;
        permission.CanSendFiles = true;
        permission.CanInviteMembers = false;
        permission.CanRemoveMembers = false;
        permission.CanEditGroupInfo = false;
        permission.CanPinMessages = false;
        permission.CanDeleteMessages = true;
        permission.CanManageCalls = false;
        await _unitOfWork.MemberPermissionRepository.UpdateAsync(permission);
    }

    private async Task GrantAdminPermissionsAsync(Guid memberId)
    {
        var permission = await _unitOfWork.MemberPermissionRepository
            .GetByMemberIdAsync(memberId);
        if (permission is null)
        {
            await _unitOfWork.MemberPermissionRepository
                .CreateDefaultAsync(memberId, isAdmin: true);
            return;
        }

        permission.CanInviteMembers = true;
        permission.CanRemoveMembers = true;
        permission.CanEditGroupInfo = true;
        permission.CanPinMessages = true;
        permission.CanManageCalls = true;
        await _unitOfWork.MemberPermissionRepository.UpdateAsync(permission);
    }

    private async Task RemoveUserConnectionsFromGroupAsync(Guid userId, Guid chatroomId)
    {
        var connections = await _connectionManager.GetConnectionsAsync(userId);
        await Task.WhenAll(connections.Select(connectionId =>
            _hubContext.Groups.RemoveFromGroupAsync(connectionId, chatroomId.ToString())));
    }

    private async Task InvalidateGroupCachesAsync(Guid chatroomId, IEnumerable<Guid> userIds)
    {
        var distinctUserIds = userIds.Distinct().ToArray();
        var tasks = distinctUserIds.SelectMany(userId => new[]
        {
            _cached.RemoveAsync(CacheKeys.ChatroomList(userId, false, null)),
            _cached.RemoveAsync(CacheKeys.ChatroomList(userId, true, null)),
            _cached.RemoveAsync(CacheKeys.ChatroomList(userId, false, "direct")),
            _cached.RemoveAsync(CacheKeys.ChatroomList(userId, false, "group")),
            _cached.RemoveAsync(CacheKeys.ChatroomList(userId, true, "direct")),
            _cached.RemoveAsync(CacheKeys.ChatroomList(userId, true, "group")),
            _cached.RemoveAsync(CacheKeys.ChatroomDetail(userId, chatroomId))
        }).Concat(new[]
        {
            _cached.RemoveAsync(CacheKeys.ChatroomMembers(chatroomId)),
            _cached.RemoveAsync(CacheKeys.Messages(chatroomId, 1, 50))
        });
        await Task.WhenAll(tasks);
    }

    private static string DisplayName(User user) =>
        user.Fullname ?? user.Username ?? "Unknown user";
}
