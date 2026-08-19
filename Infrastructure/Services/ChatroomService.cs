using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Responses.Chatrooms;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.ChatroomDTO;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Hubs;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Infrastructure.Services;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.SignalR;
using linksy_backend_api.Infrastructure.Mappers;
using ChatroomMember = linksy_backend_api.Models.ChatroomMember;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.Infrastructure.Cache;
using SixLabors.ImageSharp.Web.Caching;
using linksy_backend_api.Domain.Interfaces.Services;

namespace linksy_backend_api.Services
{
    public partial class ChatroomService : IChatroomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatroomService> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IFileService _fileService;
        private readonly ICacheService _cached;
        private readonly INotificationService _notificationService;
        private readonly IDirectContactPrivacyService _directContactPrivacy;
        public ChatroomService(
            IUnitOfWork unitOfWork,
            ILogger<ChatroomService> logger,
            IConnectionManager connectionManager,
            IHubContext<ChatHub> hubContext,
            IFileService fileService,
            ICacheService cached,
            INotificationService notificationService,
            IDirectContactPrivacyService directContactPrivacy)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _connectionManager = connectionManager;
            _hubContext = hubContext;
            _fileService = fileService;
            _cached = cached;
            _notificationService = notificationService;
            _directContactPrivacy = directContactPrivacy;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ChatroomResponseDto> CreateDirectChatroomAsync(Guid userId, Guid otherUserId)
        {
            if (userId == otherUserId)
                throw new ArgumentException("User cannot create a direct chatroom with themselves.");

            var otherUser = await _unitOfWork.Users.GetByIdAsync(otherUserId)
                ?? throw new ArgumentException("The other user does not exist.");

            var existingChatroom = await _unitOfWork.ChatroomRepository.GetDirectChatroomAsync(userId, otherUserId);
            if (existingChatroom != null)
                return await MapToChatroomResponseAsync(existingChatroom, userId);

            await _directContactPrivacy.EnsureCanContactUserAsync(userId, otherUserId, "message");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var newChatroom = new Chatroom
                {
                    ChatroomId = Guid.NewGuid(),
                    RoomType = "direct",
                    CreatedBy = userId,
                    IsActive = true,
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow
                };
                await _unitOfWork.Chatrooms.AddAsync(newChatroom);

                var member1 = new ChatroomMember { MemberId = Guid.NewGuid(), ChatroomId = newChatroom.ChatroomId, UserId = userId, MemberRole = "member", JoinedAt = DateTime.UtcNow };
                var member2 = new ChatroomMember { MemberId = Guid.NewGuid(), ChatroomId = newChatroom.ChatroomId, UserId = otherUserId, MemberRole = "member", JoinedAt = DateTime.UtcNow };
                await _unitOfWork.ChatroomMembers.AddAsync(member1);
                await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(member1.MemberId, isAdmin: false);
                await _unitOfWork.ChatroomMembers.AddAsync(member2);
                await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(member2.MemberId, isAdmin: false);

                await _unitOfWork.CommitTransactionAsync();
                return await MapToChatroomResponseAsync(newChatroom, userId);
            }
            catch { await _unitOfWork.RollbackTransactionAsync(); throw; }
        }

        public async Task<ChatroomResponseDto> CreateGroupChatroomAsync(Guid userId, CreateGroupChatroomRequest groupDto)
        {
            var distinctMembers = groupDto.MemberIds?.Where(id => id != userId).Distinct().ToList() ?? new List<Guid>();
            if (distinctMembers.Count < 2)
                throw new ArgumentException("Group chat phải có ít nhất 3 người (bạn + 2 thành viên).");

            var actor = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("Acting user does not exist.");

            await _unitOfWork.BeginTransactionAsync();
            var addedMemberIds = new List<Guid>();
            try
            {
                var newChatroom = new Chatroom
                {
                    ChatroomId = Guid.NewGuid(),
                    RoomName = groupDto.RoomName,
                    Description = groupDto.Description,
                    Avatar = DefaultAvatarHelper.GetDefaultGroupAvatar(),
                    RoomType = "group",
                    CreatedBy = userId,
                    IsActive = true,
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow
                };
                await _unitOfWork.Chatrooms.AddAsync(newChatroom);

                var adminMember = new ChatroomMember { MemberId = Guid.NewGuid(), ChatroomId = newChatroom.ChatroomId, UserId = userId, MemberRole = "admin", JoinedAt = DateTime.UtcNow };
                await _unitOfWork.ChatroomMembers.AddAsync(adminMember);
                await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(adminMember.MemberId, isAdmin: true);

                foreach (var memberId in distinctMembers)
                {
                    if (await _unitOfWork.Users.GetByIdAsync(memberId) == null) continue;
                    var member = new ChatroomMember { MemberId = Guid.NewGuid(), ChatroomId = newChatroom.ChatroomId, UserId = memberId, MemberRole = "member", AddedBy = userId, JoinedAt = DateTime.UtcNow };
                    await _unitOfWork.ChatroomMembers.AddAsync(member);
                    await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(member.MemberId, isAdmin: false);
                    addedMemberIds.Add(memberId);
                }

                await _unitOfWork.CommitTransactionAsync();
                var response = await MapToChatroomResponseAsync(newChatroom, userId);

                try
                {
                    await _notificationService.CreateBulkNotificationsAsync(
                        addedMemberIds.Select(memberId => new CreateNotificationRequest
                        {
                            UserId = memberId,
                            NotificationType = "added_to_group",
                            Title = "Đã thêm vào nhóm",
                            Body = $"{DisplayName(actor)} đã thêm bạn vào nhóm '{newChatroom.RoomName ?? "Nhóm"}'.",
                            RelatedEntityId = newChatroom.ChatroomId,
                            RelatedEntityType = "chatroom",
                            ActionUrl = $"/dashboard?chatroomId={newChatroom.ChatroomId}",
                            ImageUrl = newChatroom.Avatar
                        }).ToList());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create add-member notifications for {ChatroomId}", newChatroom.ChatroomId);
                }

                foreach (var memberId in addedMemberIds)
                {
                    var conns = await _connectionManager.GetConnectionsAsync(memberId);
                    if (conns.Any())
                        await _hubContext.Clients.Clients(conns).SendAsync("AddedToGroup", response);
                }

                return response;
            }
            catch { await _unitOfWork.RollbackTransactionAsync(); throw; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<ChatroomResponseDto>> GetUserChatroomsAsync(Guid userId, bool includeArchived = false, string? roomType = null)
        {
            var cacheKey = CacheKeys.ChatroomList(userId, includeArchived, roomType);

            var cached = await _cached.GetAsync<List<ChatroomResponseDto>>(cacheKey);
            if (cached is not null)
                return cached;

            var chatrooms = await _unitOfWork.ChatroomRepository.GetUserChatroomsAsync(userId, includeArchived, roomType);

            var result = new List<ChatroomResponseDto>();
            foreach (var c in chatrooms)
                result.Add(await ChatroomMapper.ToResponseAsync(c, userId, _unitOfWork));
            await _cached.SetAsync(cacheKey, result, CacheKeys.ShortTtl);
            return result;
        }

        public async Task<ChatroomResponseDto> GetChatroomDetailsAsync(Guid userId, Guid chatroomId)
        {
            // Fix: await thay vì .Result
            if (!await _unitOfWork.ChatroomRepository.IsUserMemberAsync(chatroomId, userId))
                throw new UnauthorizedAccessException("Bạn không phải là thành viên của chatroom này.");

            var chatroom = await _unitOfWork.ChatroomRepository.GetChatroomDetailsAsync(chatroomId)
                ?? throw new KeyNotFoundException("Chatroom không tồn tại.");

            return await MapToChatroomResponseAsync(chatroom, userId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ChatroomResponseDto> UpdateChatroomInfoAsync(Guid userId, Guid chatroomId, UpdateChatroomRequest request)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");
            if (chatroom.RoomType == "direct")
                throw new InvalidOperationException("Không thể cập nhật thông tin chat 1-1.");

            // Fix: đúng thứ tự param (chatroomId, userId)
            var caller = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId);
            var canEdit = await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, chatroomId, PermissionType.CanEditGroupInfo);

            if (!canEdit && caller?.MemberRole != "admin")
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa thông tin nhóm.");

            if (!string.IsNullOrWhiteSpace(request.RoomName)) chatroom.RoomName = request.RoomName;
            if (!string.IsNullOrWhiteSpace(request.Description)) chatroom.Description = request.Description;

            chatroom.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Chatrooms.Update(chatroom);
            await _unitOfWork.SaveChangesAsync();

            await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync("ChatroomInfoUpdated", new
            {
                ChatroomId = chatroomId,
                RoomName = chatroom.RoomName,
                Description = chatroom.Description,
                UpdatedBy = userId,
                UpdatedAt = chatroom.UpdatedAt
            });

            return await MapToChatroomResponseAsync(chatroom, userId);
        }

        public async Task<ApiResponseDto> UpdateChatroomQuickEmojiAsync(Guid userId, Guid chatroomId, string emoji)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");

            _ = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            var trimmed = (emoji ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 16)
                throw new InvalidOperationException("Emoji không hợp lệ.");

            chatroom.QuickEmoji = trimmed;
            chatroom.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Chatrooms.Update(chatroom);
            await _unitOfWork.SaveChangesAsync();

            await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync(
                "ChatroomQuickEmojiChanged",
                new { ChatroomId = chatroomId, QuickEmoji = trimmed, UpdatedBy = userId });

            return new ApiResponseDto { Success = true, Message = "Đã đổi emoji nhanh." };
        }

        public async Task<ApiResponseDto> UpdateMemberPermissionsAsync(Guid userId, Guid chatroomId, Guid memberId, UpdateMemberPermissionsRequest request)
        {
            var caller = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            if (caller.MemberRole != "admin")
                throw new UnauthorizedAccessException("Chỉ admin mới có quyền thay đổi quyền thành viên.");

            if (userId == memberId)
                throw new InvalidOperationException("Không thể tự thay đổi quyền của mình.");

            var permission = await _unitOfWork.MemberPermissionRepository.GetByUserAndChatroomAsync(memberId, chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy quyền của thành viên.");

            if (request.CanSendMessages is not null) permission.CanSendMessages = request.CanSendMessages.Value;
            if (request.CanSendMedia is not null) permission.CanSendMedia = request.CanSendMedia.Value;
            if (request.CanSendVoice is not null) permission.CanSendVoice = request.CanSendVoice.Value;
            if (request.CanSendFiles is not null) permission.CanSendFiles = request.CanSendFiles.Value;
            if (request.CanInviteMembers is not null) permission.CanInviteMembers = request.CanInviteMembers.Value;
            if (request.CanRemoveMembers is not null) permission.CanRemoveMembers = request.CanRemoveMembers.Value;
            if (request.CanEditGroupInfo is not null) permission.CanEditGroupInfo = request.CanEditGroupInfo.Value;
            if (request.CanPinMessages is not null) permission.CanPinMessages = request.CanPinMessages.Value;
            if (request.CanDeleteMessages is not null) permission.CanDeleteMessages = request.CanDeleteMessages.Value;
            if (request.CanManageCalls is not null) permission.CanManageCalls = request.CanManageCalls.Value;

            await _unitOfWork.MemberPermissionRepository.UpdateAsync(permission);
            await _unitOfWork.SaveChangesAsync();

            var targetConns = await _connectionManager.GetConnectionsAsync(memberId);
            if (targetConns.Any())
                await _hubContext.Clients.Clients(targetConns).SendAsync("PermissionsUpdated", new { ChatroomId = chatroomId, UpdatedBy = userId });

            return new ApiResponseDto { Success = true, Message = "Đã cập nhật quyền thành viên." };
        }

        public async Task<ApiResponseDto> UpdateMemberNicknameAsync(Guid userId, Guid chatroomId, Guid memberId, string? nickname)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");

            _ = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            var target = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, memberId)
                ?? throw new KeyNotFoundException("Không tìm thấy thành viên trong phòng chat.");

            var trimmed = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();
            if (trimmed is { Length: > 50 })
                throw new InvalidOperationException("Biệt danh tối đa 50 ký tự.");

            var actor = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("Acting user does not exist.");
            var targetUser = await _unitOfWork.Users.GetByIdAsync(memberId)
                ?? throw new KeyNotFoundException("Member user does not exist.");

            Message systemMessage;
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                target.Nickname = trimmed;
                _unitOfWork.ChatroomMembers.Update(target);

                var text = trimmed == null
                    ? $"{DisplayName(actor)} đã xóa biệt danh của {DisplayName(targetUser)}."
                    : userId == memberId
                        ? $"{DisplayName(actor)} đã tự đặt biệt danh là \"{trimmed}\"."
                        : $"{DisplayName(actor)} đã đặt biệt danh cho {DisplayName(targetUser)} là \"{trimmed}\".";

                systemMessage = await AddMembershipSystemMessageAsync(chatroom, userId, text);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            await InvalidateGroupCachesAsync(chatroomId, new[] { userId, memberId });
            await BroadcastMembershipSystemMessageAsync(systemMessage, userId);

            await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync(
                "MemberNicknameChanged",
                new { ChatroomId = chatroomId, MemberId = memberId, Nickname = trimmed, UpdatedBy = userId });

            return new ApiResponseDto
            {
                Success = true,
                Message = trimmed == null ? "Đã xóa biệt danh." : "Đã cập nhật biệt danh.",
            };
        }

        public async Task<AvatarResponse> UpdateGroupAvatarAsync(Guid userId, Guid chatroomId, IFormFile avatarFile)
        {
            try
            {
                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
                if (chatroom == null) return new AvatarResponse { Success = false, Message = "Không tìm thấy nhóm chat." };
                if (chatroom.RoomType == "direct") return new AvatarResponse { Success = false, Message = "Không thể thay đổi avatar của chat 1-1." };

                if (!await _unitOfWork.ChatroomMemberRepository.HasActiveMemberAsync(chatroomId, userId))
                    return new AvatarResponse { Success = false, Message = "Bạn không có quyền thay đổi avatar nhóm." };

                if (!string.IsNullOrEmpty(chatroom.Avatar) && !DefaultAvatarHelper.IsDefaultAvatar(chatroom.Avatar))
                    await _fileService.DeleteAvatarAsync(chatroom.Avatar);

                var avatarUrl = await _fileService.UploadAvatarAsync(avatarFile, "avatars/groups");
                chatroom.Avatar = avatarUrl; chatroom.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Chatrooms.Update(chatroom);
                await _unitOfWork.SaveChangesAsync();

                await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync("GroupAvatarUpdated",
                    new { ChatroomId = chatroomId, AvatarUrl = avatarUrl, UpdatedBy = userId, UpdatedAt = DateTime.UtcNow });

                return new AvatarResponse { Success = true, Message = "Cập nhật avatar nhóm thành công.", AvatarUrl = avatarUrl };
            }
            catch (Exception ex) { _logger.LogError(ex, "Error updating group avatar"); return new AvatarResponse { Success = false, Message = ex.Message }; }
        }

        public async Task<AvatarResponse> DeleteGroupAvatarAsync(Guid userId, Guid chatroomId)
        {
            try
            {
                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
                if (chatroom == null) return new AvatarResponse { Success = false, Message = "Không tìm thấy nhóm chat." };

                if (!await _unitOfWork.ChatroomMemberRepository.HasActiveMemberAsync(chatroomId, userId))
                    return new AvatarResponse { Success = false, Message = "Bạn không có quyền xóa avatar nhóm." };

                if (!string.IsNullOrEmpty(chatroom.Avatar) && !DefaultAvatarHelper.IsDefaultAvatar(chatroom.Avatar))
                    await _fileService.DeleteAvatarAsync(chatroom.Avatar);

                chatroom.Avatar = DefaultAvatarHelper.GetDefaultGroupAvatar(); chatroom.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Chatrooms.Update(chatroom);
                await _unitOfWork.SaveChangesAsync();

                await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync("GroupAvatarDeleted",
                    new { ChatroomId = chatroomId, AvatarUrl = chatroom.Avatar, DeletedBy = userId, DeletedAt = DateTime.UtcNow });

                return new AvatarResponse { Success = true, Message = "Đã đặt lại avatar mặc định.", AvatarUrl = chatroom.Avatar };
            }
            catch (Exception ex) { _logger.LogError(ex, "Error deleting group avatar"); return new AvatarResponse { Success = false, Message = ex.Message }; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // MEMBERS
        // ─────────────────────────────────────────────────────────────────────

        private async Task<ApiResponseDto> AddMembersLegacyAsync(Guid userId, Guid chatroomId, AddMembersRequest request)
        {
            //get chatroom muon add member
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
            //chatroom ko ton tai
            if (chatroom == null) return new ApiResponseDto { Success = false, Message = "Chatroom không tồn tại." };

            if (chatroom.RoomType == "direct") return new ApiResponseDto { Success = false, Message = "Không thể thêm thành viên vào chat 1-1." };

            var caller = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId);
            bool isAdmin = caller?.MemberRole == "admin";
            bool canInvite = isAdmin || await _unitOfWork.MemberPermissionRepository
                .HasPermissionAsync(userId, chatroomId, PermissionType.CanInviteMembers);
            if (!canInvite) return new ApiResponseDto { Success = false, Message = "Bạn không có quyền thêm thành viên." };

            var validUserIds = await _unitOfWork.UserRepository.GetExistingUserIdsAsync(request.MemberIds);
            var addedMemberIds = new List<Guid>();
            foreach (var memberId in validUserIds)
            {
                if (await _unitOfWork.ChatroomMemberRepository.HasActiveMemberAsync(chatroomId, memberId))
                    continue;

                var newMember = CreateMember(chatroomId, memberId, "member", addedBy: userId);
                await _unitOfWork.ChatroomMembers.AddAsync(newMember);
                await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(newMember.MemberId, isAdmin: false);
                addedMemberIds.Add(memberId);
            }
            if (addedMemberIds.Count == 0)
                return new ApiResponseDto { Success = true, Message = "Không có thành viên mới nào được thêm." };


            await _unitOfWork.SaveChangesAsync();
            await InvalidateChatroomListCacheAsync(addedMemberIds.ToArray());

            // SignalR sau khi DB đã có data
            var notifyTasks = addedMemberIds.Select(async memberId =>
            {
                var conns = await _connectionManager.GetConnectionsAsync(memberId);
                if (conns.Any())
                    await _hubContext.Clients.Clients(conns)
                        .SendAsync("AddedToGroup", await MapToChatroomResponseAsync(chatroom, memberId));
            });
            await Task.WhenAll(notifyTasks);

            try
            {
                await _hubContext.Clients.Group(chatroomId.ToString())
                    .SendAsync("MembersAdded", new { ChatroomId = chatroomId, AddedBy = userId, Count = addedMemberIds.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR failed for MembersAdded. ChatroomId={ChatroomId}", chatroomId);
            }

            return new ApiResponseDto { Success = true, Message = $"Đã thêm {addedMemberIds.Count} thành viên." };
        }

        private async Task<ApiResponseDto> RemoveMemberLegacyAsync(Guid userId, Guid chatroomId, Guid memberId)
        {
            if (userId == memberId) return new ApiResponseDto { Success = false, Message = "Dùng LeaveChatroom để tự rời nhóm." };

            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");
            if (chatroom.RoomType == "direct") return new ApiResponseDto { Success = false, Message = "Không thể xóa thành viên khỏi chat 1-1." };

            var caller = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            bool isAdmin = caller.MemberRole == "admin";
            bool canRemove = isAdmin || await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, chatroomId, PermissionType.CanRemoveMembers);
            if (!canRemove) throw new UnauthorizedAccessException("Bạn không có quyền xóa thành viên.");

            var target = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, memberId)
                ?? throw new KeyNotFoundException("Không tìm thấy thành viên trong phòng chat.");

            if (target.MemberRole == "admin" && chatroom.CreatedBy != userId)
                throw new UnauthorizedAccessException("Không thể xóa admin khác.");

            target.LeftAt = DateTime.UtcNow; target.RemovedBy = userId;
            _unitOfWork.ChatroomMembers.Update(target);
            await _unitOfWork.SaveChangesAsync();

            await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync("MemberRemoved", new { ChatroomId = chatroomId, RemovedUserId = memberId, RemovedBy = userId });
            var removedConns = await _connectionManager.GetConnectionsAsync(memberId);
            if (removedConns.Any())
                await _hubContext.Clients.Clients(removedConns).SendAsync("RemovedFromGroup", chatroomId);

            return new ApiResponseDto { Success = true, Message = "Đã xóa thành viên." };
        }

        private async Task<ApiResponseDto> LeaveChatroomLegacyAsync(Guid userId, Guid chatroomId)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");
            if (chatroom.RoomType == "direct")
                return new ApiResponseDto { Success = false, Message = "Không thể rời khỏi chat 1-1." };

            var member = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new InvalidOperationException("Bạn không phải thành viên của phòng chat này.");

            // FIX: wrap toàn bộ trong 1 transaction — tránh inconsistent state
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (member.MemberRole == "admin" &&
                    !await _unitOfWork.ChatroomMemberRepository.HasOtherAdminAsync(chatroomId, userId))
                {
                    var next = await _unitOfWork.ChatroomMemberRepository
                        .GetNextMemberToPromoteAsync(chatroomId, userId);

                    if (next == null)
                    {
                        // Không còn ai → giải tán phòng
                        chatroom.IsActive = false;
                        _unitOfWork.Chatrooms.Update(chatroom);
                        member.LeftAt = DateTime.UtcNow;
                        _unitOfWork.ChatroomMembers.Update(member);
                        await _unitOfWork.CommitTransactionAsync();

                        await InvalidateChatroomListCacheAsync(userId);
                        return new ApiResponseDto { Success = true, Message = "Đã rời và giải tán phòng chat." };
                    }

                    // Promote member tiếp theo lên admin
                    next.MemberRole = "admin";
                    _unitOfWork.ChatroomMembers.Update(next);

                    var nextPerm = await _unitOfWork.MemberPermissionRepository.GetByMemberIdAsync(next.MemberId);
                    if (nextPerm != null)
                    {
                        nextPerm.CanInviteMembers = true;
                        nextPerm.CanRemoveMembers = true;
                        nextPerm.CanEditGroupInfo = true;
                        nextPerm.CanPinMessages = true;
                        nextPerm.CanManageCalls = true;
                        await _unitOfWork.MemberPermissionRepository.UpdateAsync(nextPerm);
                    }

                    // Notify member được promote (trong try để lỗi SignalR không block)
                    _ = NotifyPromotedMemberAsync(next.UserId, chatroomId);
                }

                member.LeftAt = DateTime.UtcNow;
                _unitOfWork.ChatroomMembers.Update(member);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error leaving chatroom. UserId={UserId}, ChatroomId={ChatroomId}",
                    userId, chatroomId);
                throw;
            }

            await InvalidateChatroomListCacheAsync(userId);

            try
            {
                await _hubContext.Clients.Group(chatroomId.ToString())
                    .SendAsync("MemberLeft", new { ChatroomId = chatroomId, UserId = userId });

                var userConns = await _connectionManager.GetConnectionsAsync(userId);
                if (userConns.Any())
                    await _hubContext.Clients.Clients(userConns).SendAsync("RemovedFromGroup", chatroomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR failed for MemberLeft. UserId={UserId}", userId);
            }

            return new ApiResponseDto { Success = true, Message = "Đã rời phòng chat." };
        }

        public async Task<ApiResponseDto> ArchiveChatroomAsync(Guid userId, Guid chatroomId, bool isArchived)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");

            if (!await _unitOfWork.ChatroomRepository.IsUserMemberAsync(chatroomId, userId))
                throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            chatroom.IsArchived = isArchived;
            chatroom.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Chatrooms.Update(chatroom);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate cả 2 variant (archived và non-archived)
            await InvalidateChatroomListCacheAsync(userId);

            return new ApiResponseDto
            {
                Success = true,
                Message = isArchived ? "Đã lưu trữ phòng chat." : "Đã bỏ lưu trữ phòng chat."
            };
        }

        public async Task<ApiResponseDto> PinChatroomAsync(Guid userId, Guid chatroomId, bool isPinned)
        {
            var member = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            member.IsPinned = isPinned;
            member.PinnedAt = isPinned ? DateTime.UtcNow : null;
            _unitOfWork.ChatroomMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();
            await InvalidateChatroomListCacheAsync(userId);

            return new ApiResponseDto
            {
                Success = true,
                Message = isPinned ? "Đã ghim hội thoại." : "Đã bỏ ghim hội thoại."
            };
        }

        public async Task<ApiResponseDto> MuteChatroomAsync(
            Guid userId, Guid chatroomId, bool isMuted, DateTime? muteUntil = null)
        {
            var member = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            member.IsMuted = isMuted;
            member.MutedUntil = isMuted ? muteUntil : null;
            member.NotificationPreference = isMuted ? "mute" : "all";
            _unitOfWork.ChatroomMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();
            await InvalidateChatroomListCacheAsync(userId);

            return new ApiResponseDto
            {
                Success = true,
                Message = isMuted ? "Đã tắt thông báo hội thoại." : "Đã bật lại thông báo hội thoại.",
                Data = new
                {
                    IsMuted = isMuted,
                    MutedUntil = member.MutedUntil,
                    NotificationPreference = member.NotificationPreference
                }
            };
        }

        public async Task<ApiResponseDto> ClearConversationAsync(Guid userId, Guid chatroomId)
        {
            var member = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            var clearedAt = DateTime.UtcNow;
            member.ClearedAt = clearedAt;
            member.LastReadAt = clearedAt;
            member.IsPinned = false;
            member.PinnedAt = null;
            _unitOfWork.ChatroomMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();
            await InvalidateChatroomListCacheAsync(userId);

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đã xóa hội thoại khỏi danh sách của bạn.",
                Data = new { ClearedAt = clearedAt }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────
        // MAPPER — delegate to ChatroomMapper
        // ─────────────────────────────────────────────────────────────────────
        private static ChatroomMember CreateMember(
            Guid chatroomId, Guid userId, string role, Guid? addedBy = null) => new()
            {
                MemberId = Guid.NewGuid(),
                ChatroomId = chatroomId,
                UserId = userId,
                MemberRole = role,
                AddedBy = addedBy,
                JoinedAt = DateTime.UtcNow
            };
        // Invalidate ChatroomList cache cho nhiều variant (includeArchived + roomType)
        private async Task InvalidateChatroomListCacheAsync(params Guid[] userIds)
        {
            var tasks = userIds.SelectMany(uid => new[]
            {
                _cached.RemoveAsync(CacheKeys.ChatroomList(uid, false, null)),
                _cached.RemoveAsync(CacheKeys.ChatroomList(uid, true, null)),
                _cached.RemoveAsync(CacheKeys.ChatroomList(uid, false, "direct")),
                _cached.RemoveAsync(CacheKeys.ChatroomList(uid, false, "group")),
                _cached.RemoveAsync(CacheKeys.ChatroomList(uid, true, "direct")),
                _cached.RemoveAsync(CacheKeys.ChatroomList(uid, true, "group")),
            });
            await Task.WhenAll(tasks);
        }
        // Fire-and-forget notify cho member được promote — không block main flow
        private async Task NotifyPromotedMemberAsync(Guid userId, Guid chatroomId)
        {
            try
            {
                var conns = await _connectionManager.GetConnectionsAsync(userId);
                if (conns.Any())
                    await _hubContext.Clients.Clients(conns)
                        .SendAsync("PromotedToAdmin", new { ChatroomId = chatroomId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR failed for PromotedToAdmin. UserId={UserId}", userId);
            }
        }
        private Task<ChatroomResponseDto> MapToChatroomResponseAsync(Chatroom chatroom, Guid currentUserId)
            => ChatroomMapper.ToResponseAsync(chatroom, currentUserId, _unitOfWork);
    }
}
