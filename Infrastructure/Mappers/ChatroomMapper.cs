using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.Domain.DTOs.Responses.Chatrooms;
using linksy_backend_api.DTOs.ChatroomDTO;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;

namespace linksy_backend_api.Infrastructure.Mappers
{
    public static class ChatroomMapper
    {
        public static async Task<ChatroomResponseDto> ToResponseAsync(
            Chatroom chatroom,
            Guid currentUserId,
            IUnitOfWork unitOfWork)
        {
            var members = await unitOfWork.ChatroomMemberRepository.GetActiveMembersWithUserAsync(chatroom.ChatroomId);
            var membersByUserId = members
                .Where(m => m.LeftAt == null)
                .GroupBy(m => m.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            var memberDtos = members.Where(m => m.LeftAt == null).Select(m =>
            {
                ChatroomMember? addedByMember = null;
                if (m.AddedBy.HasValue)
                    membersByUserId.TryGetValue(m.AddedBy.Value, out addedByMember);

                return new ChatroomMemberRequest
                {
                    UserId = m.UserId,
                    Username = m.User?.Username ?? string.Empty,
                    Fullname = m.User?.Fullname ?? string.Empty,
                    Avatar = DefaultAvatarHelper.GetAvatarOrDefault(m.User?.Avatar, m.UserId, username: m.User?.Username, fullname: m.User?.Fullname),
                    MemberRole = m.MemberRole,
                    Nickname = m.Nickname,
                    JoinedAt = m.JoinedAt ?? DateTime.UtcNow,
                    LastReadAt = m.LastReadAt,
                    AddedBy = m.AddedBy,
                    AddedByUsername = addedByMember?.User?.Username,
                    AddedByFullname = addedByMember?.User?.Fullname
                };
            }).ToList();

            var myMembership = members.FirstOrDefault(m => m.UserId == currentUserId);
            var clearedAt = myMembership?.ClearedAt;

            // Last message (respect per-user clear)
            MessageResponse? lastMessageDto = null;
            if (chatroom.LastMessageId != null)
            {
                var lastMessage = await unitOfWork.Messages.GetByIdAsync(chatroom.LastMessageId.Value);
                if (lastMessage != null &&
                    (clearedAt == null || lastMessage.SentAt > clearedAt))
                {
                    lastMessageDto = await MessageMapper.ToResponseAsync(lastMessage, unitOfWork);
                }
                else if (clearedAt != null)
                {
                    var visibleLast = await unitOfWork.MessageRepository.GetLastMessageAfterAsync(
                        chatroom.ChatroomId, clearedAt.Value);
                    if (visibleLast != null)
                        lastMessageDto = await MessageMapper.ToResponseAsync(visibleLast, unitOfWork);
                }
            }

            // Unread count — baseline is max(LastReadAt, ClearedAt, JoinedAt)
            var unreadCount = 0;
            if (myMembership != null)
            {
                var lastRead = myMembership.LastReadAt ?? myMembership.JoinedAt ?? DateTime.UtcNow;
                if (clearedAt.HasValue && clearedAt.Value > lastRead)
                    lastRead = clearedAt.Value;
                unreadCount = await unitOfWork.MessageRepository.GetUnreadCountAsync(
                    chatroom.ChatroomId, currentUserId, lastRead);
            }

            // MyMemberInfo + permissions
            MemberInfoResponseDto? myMemberInfo = null;
            if (myMembership != null)
            {
                var preference = myMembership.NotificationPreference ?? "all";
                var isSelfMuted = (myMembership.IsMuted ?? false) ||
                    string.Equals(preference, "mute", StringComparison.OrdinalIgnoreCase);
                var permission = await unitOfWork.MemberPermissionRepository.GetByMemberIdAsync(myMembership.MemberId);
                myMemberInfo = new MemberInfoResponseDto
                {
                    MemberId = myMembership.MemberId,
                    MemberRole = myMembership.MemberRole,
                    IsOwner = myMembership.UserId == chatroom.CreatedBy,
                    Nickname = myMembership.Nickname,
                    IsMuted = isSelfMuted,
                    MutedUntil = myMembership.MutedUntil,
                    NotificationPreference = preference,
                    IsPinned = myMembership.IsPinned,
                    PinnedAt = myMembership.PinnedAt,
                    ClearedAt = myMembership.ClearedAt,
                    MessageCount = myMembership.MessageCount ?? 0,
                    LastReadAt = myMembership.LastReadAt,
                    JoinedAt = myMembership.JoinedAt ?? DateTime.UtcNow,
                    Permissions = permission == null ? new MemberPermissionResponseDto() : new MemberPermissionResponseDto
                    {
                        CanSendMessages = permission.CanSendMessages,
                        CanSendMedia = permission.CanSendMedia,
                        CanSendVoice = permission.CanSendVoice,
                        CanSendFiles = permission.CanSendFiles,
                        CanInviteMembers = permission.CanInviteMembers,
                        CanRemoveMembers = permission.CanRemoveMembers,
                        CanEditGroupInfo = permission.CanEditGroupInfo,
                        CanPinMessages = permission.CanPinMessages,
                        CanDeleteMessages = permission.CanDeleteMessages,
                        CanManageCalls = permission.CanManageCalls
                    }
                };
            }

            // Direct chat: dùng tên/avatar của người kia
            var roomName = chatroom.RoomName;
            var avatar = chatroom.Avatar;
            if (chatroom.RoomType == "direct")
            {
                var other = members.FirstOrDefault(m => m.UserId != currentUserId);
                if (other != null)
                {
                    roomName = other.User?.Fullname ?? other.User?.Username ?? string.Empty;
                    avatar = DefaultAvatarHelper.GetAvatarOrDefault(other.User?.Avatar, other.UserId, username: other.User?.Username, fullname: other.User?.Fullname);
                }
                else { avatar = DefaultAvatarHelper.GetAvatarOrDefault(chatroom.Avatar); }
            }

            return new ChatroomResponseDto
            {
                ChatroomId = chatroom.ChatroomId,
                CreatedBy = chatroom.CreatedBy,
                RoomName = roomName ?? string.Empty,
                QuickEmoji = string.IsNullOrWhiteSpace(chatroom.QuickEmoji) ? "👍" : chatroom.QuickEmoji,
                Description = chatroom.Description ?? string.Empty,
                Avatar = avatar ?? string.Empty,
                RoomType = chatroom.RoomType,
                IsActive = chatroom.IsActive ?? true,
                IsArchived = chatroom.IsArchived ?? false,
                CreatedAt = chatroom.CreatedAt ?? DateTime.UtcNow,
                LastActivityAt = chatroom.LastActivityAt,
                LastMessage = lastMessageDto ?? new MessageResponse(),
                Members = memberDtos,
                UnreadCount = unreadCount,
                MyMemberInfo = myMemberInfo
            };
        }
    }
}