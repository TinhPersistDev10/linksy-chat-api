using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.Core.Interfaces.Services;
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
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatroomMember = linksy_backend_api.Models.ChatroomMember;

namespace linksy_backend_api.Services
{
    public class ChatroomService : IChatroomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatroomService> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IMessageService _messageService;
        private readonly IFileService _fileService;
        public ChatroomService(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<ChatroomService> logger, IConnectionManager connectionManager, IHubContext<ChatHub> hubContext, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _connectionManager = connectionManager;
            _hubContext = hubContext;
            _fileService = fileService;
            _messageService = messageService;
        }



        public async Task<ApiResponseDto> AddMembersAsync(Guid userId, Guid chatroomId, AddMembersRequest request)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
            if (chatroom == null)
                return new ApiResponseDto { Success = false, Message = "Chatroom không tồn tại" };

            if (chatroom.RoomType == "direct")
                return new ApiResponseDto { Success = false, Message = "Không thể thêm thành viên vào chat 1-1" };

            var canInvite = await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, chatroomId, "CanInviteMembers");

            if (!canInvite)
                return new ApiResponseDto { Success = false, Message = "Bạn không có quyền thêm thành viên" };

            var addedCount = 0;
            foreach (var memberId in request.MemberIds)
            {
                var exists = await _unitOfWork.ChatroomMembers.AnyAsync(
                    rm => rm.ChatroomId == chatroomId && rm.UserId == memberId && rm.LeftAt == null);
                if (exists) continue;

                var user = await _unitOfWork.Users.GetByIdAsync(memberId);
                if (user == null) continue;

                await _unitOfWork.ChatroomMembers.AddAsync(new ChatroomMember
                {
                    MemberId = Guid.NewGuid(),
                    ChatroomId = chatroomId,
                    UserId = memberId,
                    MemberRole = "member",
                    JoinedAt = DateTime.UtcNow
                });
                addedCount++;

                // Notify new member
                var conns = await _connectionManager.GetConnectionsAsync(memberId);
                if (conns.Any())
                {
                    var resp = await MapToChatroomResponseAsync(chatroom, memberId);
                    await _hubContext.Clients.Clients(conns).SendAsync("AddedToGroup", resp);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _hubContext.Clients.Group(chatroomId.ToString()).SendAsync("MembersAdded", new
            {
                ChatroomId = chatroomId,
                AddedBy = userId,
                Count = addedCount
            });

            return new ApiResponseDto { Success = true, Message = $"Đã thêm {addedCount} thành viên" };
        }



        public async Task<ChatroomResponseDto> CreateDirectChatroomAsync(Guid userId, Guid otherUserId)
        {

            if (userId == otherUserId)
            {
                throw new ArgumentException("User cannot create a direct chatroom with themselves.");
            }
            var otherUser = await _unitOfWork.Users.GetByIdAsync(otherUserId);
            if (otherUser == null)
            {
                throw new ArgumentException("The other user does not exist.");
            }
            var existingChatroom = await _unitOfWork.Chatrooms.Query()
            .Where(c => c.RoomType == "direct")
            .Where(c => c.ChatroomMembers.Any(m => m.UserId == userId) && c.ChatroomMembers.Any(m => m.UserId == otherUserId))
            .FirstOrDefaultAsync();
            if (existingChatroom != null)
            {
                return await MapToChatroomResponseAsync(existingChatroom, userId);
            }

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

                var member1 = new ChatroomMember
                {
                    MemberId = Guid.NewGuid(), // Thêm MemberId
                    ChatroomId = newChatroom.ChatroomId,
                    UserId = userId,
                    MemberRole = "member", // Set trực tiếp role name
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.ChatroomMembers.AddAsync(member1);
                await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(member1.MemberId, isAdmin: false);

                var member2 = new ChatroomMember
                {
                    MemberId = Guid.NewGuid(), // Thêm MemberId
                    ChatroomId = newChatroom.ChatroomId,
                    UserId = otherUserId,
                    MemberRole = "member", // Set trực tiếp role name
                    JoinedAt = DateTime.UtcNow
                };

                await _unitOfWork.ChatroomMembers.AddAsync(member2);
                await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(member2.MemberId, isAdmin: false);
                await _unitOfWork.CommitTransactionAsync();

                return await MapToChatroomResponseAsync(newChatroom, userId);
            }
            catch (System.Exception)
            {

                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

        }

        public async Task<ChatroomResponseDto> CreateGroupChatroomAsync(Guid userId, CreateGroupChatroomRequest groupDto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Tạo chatroom mới
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

                // Thêm người tạo làm admin
                var adminMember = new ChatroomMember
                {
                    MemberId = Guid.NewGuid(),
                    ChatroomId = newChatroom.ChatroomId,
                    UserId = userId,
                    MemberRole = "admin",
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.ChatroomMembers.AddAsync(adminMember);

                // Thêm các thành viên khác (nếu có)
                if (groupDto.MemberIds != null && groupDto.MemberIds.Any())
                {
                    foreach (var memberId in groupDto.MemberIds.Where(id => id != userId))
                    {
                        // Kiểm tra user tồn tại
                        var memberUser = await _unitOfWork.Users.GetByIdAsync(memberId);
                        if (memberUser == null) continue;

                        var member = new ChatroomMember
                        {
                            MemberId = Guid.NewGuid(),
                            ChatroomId = newChatroom.ChatroomId,
                            UserId = memberId,
                            MemberRole = "member",
                            JoinedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.ChatroomMembers.AddAsync(member);
                    }
                }

                await _unitOfWork.CommitTransactionAsync();

                // Thông báo realtime cho các thành viên được thêm
                var chatroomResponse = await MapToChatroomResponseAsync(newChatroom, userId);

                if (groupDto.MemberIds != null)
                {
                    foreach (var memberId in groupDto.MemberIds.Where(id => id != userId))
                    {
                        var connections = await _connectionManager.GetConnectionsAsync(memberId);
                        if (connections.Any())
                        {
                            await _hubContext.Clients
                                .Clients(connections)
                                .SendAsync("AddedToGroup", chatroomResponse);
                        }
                    }
                }

                return chatroomResponse;
            }
            catch (System.Exception)
            {

                throw;
            }
        }



        public async Task<List<ChatroomResponseDto>> GetUserChatroomsAsync(Guid userId)
        {
            // 1. Get data from repository
            var chatrooms = await _unitOfWork.ChatroomRepository.GetUserChatroomsAsync(userId);

            var result = new List<ChatroomResponseDto>();
            foreach (var chatroom in chatrooms)
            {
                result.Add(await MapToChatroomResponseAsync(chatroom, userId));
            }

            return result;
        }

        public async Task<ChatroomResponseDto> MapToChatroomResponseAsync(Chatroom chatroom, Guid currentUserId)
        {
            // Lấy members
            var members = await _unitOfWork.ChatroomMembers.Query()
                .Where(rm => rm.ChatroomId == chatroom.ChatroomId)
                .Include(rm => rm.User)
                .ToListAsync();

            var memberDtos = members.Select(m => new ChatroomMemberRequest
            {
                UserId = m.UserId,
                Username = m.User?.Username ?? string.Empty,
                Fullname = m.User?.Fullname ?? string.Empty,
                Avatar = DefaultAvatarHelper.GetDefaultUserAvatar(m.UserId, username: m.User?.Username, fullname: m.User?.Fullname),
                MemberRole = m.MemberRole,
                JoinedAt = m.JoinedAt ?? DateTime.UtcNow
            }).ToList();

            // Lấy last message
            MessageResponse? lastMessageDto = null;
            if (chatroom.LastMessageId != null)
            {
                var lastMessage = await _unitOfWork.Messages.GetByIdAsync(chatroom.LastMessageId.Value);
                if (lastMessage != null)
                {
                    lastMessageDto = await _messageService.MapToMessageResponseAsync(lastMessage);
                }
            }

            // Tính unread count
            var myMembership = members.FirstOrDefault(m => m.UserId == currentUserId);
            var unreadCount = 0;
            if (myMembership != null)
            {
                unreadCount = await _unitOfWork.Messages.Query()
                    .Where(m => m.ChatroomId == chatroom.ChatroomId)
                    .Where(m => m.SenderId != currentUserId)
                    .Where(m => myMembership.LeftAt == null || m.SentAt > myMembership.LeftAt)
                    .CountAsync();
            }

            // Nếu là direct chat, lấy thông tin người kia làm RoomName
            var roomName = chatroom.RoomName;
            var avatar = chatroom.Avatar;
            if (chatroom.RoomType == "direct")
            {
                var otherMember = members.FirstOrDefault(m => m.UserId != currentUserId);
                if (otherMember != null)
                {
                    roomName = otherMember.User?.Fullname ?? otherMember.User?.Username;
                    avatar = DefaultAvatarHelper.GetAvatarOrDefault(
                        otherMember.User?.Avatar,
                         otherMember.UserId,
                        username: otherMember.User?.Username,
                        fullname: otherMember.User?.Fullname
                    );
                }
                else
                {
                    avatar = DefaultAvatarHelper.GetAvatarOrDefault(chatroom.Avatar);
                }
            }

            return new ChatroomResponseDto
            {
                ChatroomId = chatroom.ChatroomId,
                RoomName = roomName ?? string.Empty,
                Description = chatroom.Description ?? string.Empty,
                Avatar = avatar,
                RoomType = chatroom.RoomType,
                IsActive = chatroom.IsActive ?? true,
                IsArchived = chatroom.IsArchived ?? false,
                CreatedAt = chatroom.CreatedAt ?? DateTime.UtcNow,
                LastActivityAt = chatroom.LastActivityAt,
                LastMessage = lastMessageDto ?? new MessageResponse(),
                Members = memberDtos,
                UnreadCount = unreadCount
            };
        }



        public async Task<AvatarResponse> UpdateGroupAvatarAsync(Guid userId, Guid chatroomId, IFormFile avatarFile)
        {
            try
            {
                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
                if (chatroom == null)
                {
                    return new AvatarResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy nhóm chat"
                    };
                }

                if (chatroom.RoomType == "direct")
                {
                    return new AvatarResponse
                    {
                        Success = false,
                        Message = "Không thể thay đổi avatar của chat 1-1"
                    };
                }

                // Kiểm tra quyền
                var member = await _unitOfWork.ChatroomMembers.FirstOrDefaultAsync(
                    rm => rm.ChatroomId == chatroomId &&
                          rm.UserId == userId &&
                          rm.LeftAt == null);

                if (member == null)
                {
                    return new AvatarResponse
                    {
                        Success = false,
                        Message = "Bạn không có quyền thay đổi avatar nhóm"
                    };
                }

                // Xóa avatar cũ
                if (!string.IsNullOrEmpty(chatroom.Avatar))
                {
                    await _fileService.DeleteAvatarAsync(chatroom.Avatar);
                }

                // Upload avatar mới
                var avatarUrl = await _fileService.UploadAvatarAsync(avatarFile, "avatars/groups");

                // Cập nhật database
                chatroom.Avatar = avatarUrl;
                chatroom.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Chatrooms.Update(chatroom);
                await _unitOfWork.SaveChangesAsync();

                // Thông báo realtime cho members
                await _hubContext.Clients.Group(chatroomId.ToString())
                    .SendAsync("GroupAvatarUpdated", new
                    {
                        ChatroomId = chatroomId,
                        AvatarUrl = avatarUrl,
                        UpdatedBy = userId,
                        UpdatedAt = DateTime.UtcNow
                    });

                return new AvatarResponse
                {
                    Success = true,
                    Message = "Cập nhật avatar nhóm thành công",
                    AvatarUrl = avatarUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group avatar");
                return new AvatarResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<AvatarResponse> DeleteGroupAvatarAsync(Guid userId, Guid chatroomId)
        {
            try
            {
                var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId);
                if (chatroom == null)
                {
                    return new AvatarResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy nhóm chat"
                    };
                }

                // Kiểm tra quyền
                var member = await _unitOfWork.ChatroomMembers.FirstOrDefaultAsync(
                    rm => rm.ChatroomId == chatroomId &&
                          rm.UserId == userId &&
                          rm.LeftAt == null);

                if (member == null)
                {
                    return new AvatarResponse
                    {
                        Success = false,
                        Message = "Bạn không có quyền xóa avatar nhóm"
                    };
                }

                // Xóa file avatar nếu không phải default
                if (!string.IsNullOrEmpty(chatroom.Avatar) &&
                    !DefaultAvatarHelper.IsDefaultAvatar(chatroom.Avatar))
                {
                    await _fileService.DeleteAvatarAsync(chatroom.Avatar);
                }

                // ⭐ Set về avatar mặc định
                chatroom.Avatar = DefaultAvatarHelper.GetDefaultGroupAvatar();
                chatroom.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Chatrooms.Update(chatroom);
                await _unitOfWork.SaveChangesAsync();

                // Thông báo realtime
                await _hubContext.Clients.Group(chatroomId.ToString())
                    .SendAsync("GroupAvatarDeleted", new
                    {
                        ChatroomId = chatroomId,
                        AvatarUrl = chatroom.Avatar,
                        DeletedBy = userId,
                        DeletedAt = DateTime.UtcNow
                    });

                return new AvatarResponse
                {
                    Success = true,
                    Message = "Đã đặt lại avatar mặc định",
                    AvatarUrl = chatroom.Avatar
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group avatar");
                return new AvatarResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        public Task<ApiResponseDto> RejectGroupInvitationAsync(Guid userId, Guid invitationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponseDto> RemoveMemberAsync(Guid userId, Guid chatroomId, Guid memberId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponseDto> SendGroupInvitationAsync(Guid userId, Guid chatroomId, SendGroupInvitationRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<ChatroomResponseDto> UpdateChatroomInfoAsync(Guid userId, Guid chatroomId, UpdateChatroomRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponseDto> UpdateMemberPermissionsAsync(Guid userId, Guid chatroomId, Guid memberId, UpdateMemberPermissionsRequest request)
        {
            throw new NotImplementedException();
        }
        public Task<ApiResponseDto> AcceptGroupInvitationAsync(Guid userId, Guid invitationId)
        {
            throw new NotImplementedException();
        }
        public Task<ApiResponseDto> LeaveChatroomAsync(Guid userId, Guid chatroomId)
        {
            throw new NotImplementedException();
        }
        public Task<ChatroomResponseDto> GetChatroomDetailsAsync(Guid userId, Guid chatroomId)
        {
            throw new NotImplementedException();
        }

        public Task<List<GroupInvitationRequest>> GetReceivedInvitationsAsync(Guid userId)
        {
            throw new NotImplementedException();
        }
        public Task<ApiResponseDto> ArchiveChatroomAsync(Guid userId, Guid chatroomId, bool isArchived)
        {
            throw new NotImplementedException();
        }
        public Task<ApiResponseDto> UpdateMemberPermissionAsync(Guid userId, Guid chatroomId, Guid memberId, UpdateMemberPermissionsRequest request)
        {
            throw new NotImplementedException();
        }
    }
}