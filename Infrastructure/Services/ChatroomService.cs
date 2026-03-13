using linksy_backend_api.Core.DTOs.Responses.Users;
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

namespace linksy_backend_api.Services
{
    public class ChatroomService : IChatroomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatroomService> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IFileService _fileService;

        public ChatroomService(
            IUnitOfWork unitOfWork,
            ILogger<ChatroomService> logger,
            IConnectionManager connectionManager,
            IHubContext<ChatHub> hubContext,
            IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _connectionManager = connectionManager;
            _hubContext = hubContext;
            _fileService = fileService;
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

            await _unitOfWork.BeginTransactionAsync();
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
                }

                await _unitOfWork.CommitTransactionAsync();
                var response = await MapToChatroomResponseAsync(newChatroom, userId);

                foreach (var memberId in distinctMembers)
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
            var chatrooms = await _unitOfWork.ChatroomRepository.GetUserChatroomsAsync(userId, includeArchived, roomType);
            var result = new List<ChatroomResponseDto>();
            foreach (var c in chatrooms)
                result.Add(await ChatroomMapper.ToResponseAsync(c, userId, _unitOfWork));
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
            var canEdit = await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, chatroomId, "CanEditGroupInfo");

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

        public async Task<ApiResponseDto> AddMembersAsync(Guid userId, Guid chatroomId, AddMembersRequest request)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
            if (chatroom == null) return new ApiResponseDto { Success = false, Message = "Chatroom không tồn tại." };
            if (chatroom.RoomType == "direct") return new ApiResponseDto { Success = false, Message = "Không thể thêm thành viên vào chat 1-1." };

            var caller = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId);
            bool isAdmin = caller?.MemberRole == "admin";
            bool canInvite = isAdmin || await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, chatroomId, "CanInviteMembers");
            if (!canInvite) return new ApiResponseDto { Success = false, Message = "Bạn không có quyền thêm thành viên." };

            var addedCount = 0;
            foreach (var memberId in request.MemberIds)
            {
                if (await _unitOfWork.ChatroomMemberRepository.HasActiveMemberAsync(chatroomId, memberId)) continue;
                if (await _unitOfWork.Users.GetByIdAsync(memberId) == null) continue;

                var newMember = new ChatroomMember { MemberId = Guid.NewGuid(), ChatroomId = chatroomId, UserId = memberId, MemberRole = "member", AddedBy = userId, JoinedAt = DateTime.UtcNow };
                await _unitOfWork.ChatroomMembers.AddAsync(newMember);
                await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(newMember.MemberId, isAdmin: false);
                addedCount++;

                var conns = await _connectionManager.GetConnectionsAsync(memberId);
                if (conns.Any())
                    await _hubContext.Clients.Clients(conns).SendAsync("AddedToGroup", await MapToChatroomResponseAsync(chatroom, memberId));
            }

            await _unitOfWork.SaveChangesAsync();
            await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync("MembersAdded", new { ChatroomId = chatroomId, AddedBy = userId, Count = addedCount });
            return new ApiResponseDto { Success = true, Message = $"Đã thêm {addedCount} thành viên." };
        }

        public async Task<ApiResponseDto> RemoveMemberAsync(Guid userId, Guid chatroomId, Guid memberId)
        {
            if (userId == memberId) return new ApiResponseDto { Success = false, Message = "Dùng LeaveChatroom để tự rời nhóm." };

            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");
            if (chatroom.RoomType == "direct") return new ApiResponseDto { Success = false, Message = "Không thể xóa thành viên khỏi chat 1-1." };

            var caller = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            bool isAdmin = caller.MemberRole == "admin";
            bool canRemove = isAdmin || await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, chatroomId, "CanRemoveMembers");
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

        public async Task<ApiResponseDto> LeaveChatroomAsync(Guid userId, Guid chatroomId)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");
            if (chatroom.RoomType == "direct") return new ApiResponseDto { Success = false, Message = "Không thể rời khỏi chat 1-1." };

            var member = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(chatroomId, userId)
                ?? throw new InvalidOperationException("Bạn không phải thành viên của phòng chat này.");

            if (member.MemberRole == "admin" && !await _unitOfWork.ChatroomMemberRepository.HasOtherAdminAsync(chatroomId, userId))
            {
                var next = await _unitOfWork.ChatroomMemberRepository.GetNextMemberToPromoteAsync(chatroomId, userId);
                if (next == null)
                {
                    chatroom.IsActive = false; _unitOfWork.Chatrooms.Update(chatroom);
                    member.LeftAt = DateTime.UtcNow; _unitOfWork.ChatroomMembers.Update(member);
                    await _unitOfWork.SaveChangesAsync();
                    return new ApiResponseDto { Success = true, Message = "Đã rời và giải tán phòng chat." };
                }

                next.MemberRole = "admin";
                _unitOfWork.ChatroomMembers.Update(next);

                var nextPerm = await _unitOfWork.MemberPermissionRepository.GetByMemberIdAsync(next.MemberId);
                if (nextPerm != null)
                {
                    nextPerm.CanInviteMembers = true; nextPerm.CanRemoveMembers = true;
                    nextPerm.CanEditGroupInfo = true; nextPerm.CanPinMessages = true;
                    nextPerm.CanManageCalls = true;
                    await _unitOfWork.MemberPermissionRepository.UpdateAsync(nextPerm);
                }
            }

            member.LeftAt = DateTime.UtcNow;
            _unitOfWork.ChatroomMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();

            await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync("MemberLeft", new { ChatroomId = chatroomId, UserId = userId });
            var userConns = await _connectionManager.GetConnectionsAsync(userId);
            if (userConns.Any())
                await _hubContext.Clients.Clients(userConns).SendAsync("RemovedFromGroup", chatroomId);

            return new ApiResponseDto { Success = true, Message = "Đã rời phòng chat." };
        }

        public async Task<ApiResponseDto> ArchiveChatroomAsync(Guid userId, Guid chatroomId, bool isArchived)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");

            if (!await _unitOfWork.ChatroomRepository.IsUserMemberAsync(chatroomId, userId))
                throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            chatroom.IsArchived = isArchived; chatroom.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Chatrooms.Update(chatroom);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponseDto { Success = true, Message = isArchived ? "Đã lưu trữ phòng chat." : "Đã bỏ lưu trữ phòng chat." };
        }

        // ─────────────────────────────────────────────────────────────────────
        // ─────────────────────────────────────────────────────────────────────
        // MAPPER — delegate to ChatroomMapper
        // ─────────────────────────────────────────────────────────────────────

        private Task<ChatroomResponseDto> MapToChatroomResponseAsync(Chatroom chatroom, Guid currentUserId)
            => ChatroomMapper.ToResponseAsync(chatroom, currentUserId, _unitOfWork);
    }
}