using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.ChatroomDTO;
using linksy_backend_api.Hubs;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.SignalR;

namespace linksy_backend_api.Infrastructure.Services
{
    public class GroupInvitationService : IGroupInvitationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IConnectionManager _connectionManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<GroupInvitationService> _logger;

        public GroupInvitationService(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> hubContext,
            IConnectionManager connectionManager,
            INotificationService notificationService,
            ILogger<GroupInvitationService> logger)
        {
            _unitOfWork        = unitOfWork;
            _hubContext        = hubContext;
            _connectionManager = connectionManager;
            _notificationService = notificationService;
            _logger            = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEND
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> SendGroupInvitationAsync(Guid userId, Guid chatroomId, SendGroupInvitationRequest request)
        {
            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng chat.");

            if (chatroom.RoomType == "direct")
                return new ApiResponseDto { Success = false, Message = "Không thể gửi lời mời vào chat 1-1." };

            var caller = await _unitOfWork.ChatroomMemberRepository.GetActiveMemberAsync(userId,chatroomId)
                ?? throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            bool isAdmin   = caller.MemberRole == "admin";
            bool canInvite = isAdmin || await _unitOfWork.MemberPermissionRepository.HasPermissionAsync(userId, chatroomId, PermissionType.CanInviteMembers);
            if (!canInvite)
                throw new UnauthorizedAccessException("Bạn không có quyền mời thành viên.");

            if (await _unitOfWork.ChatroomMemberRepository.HasActiveMemberAsync(chatroomId, request.InvitedUserId))
                return new ApiResponseDto { Success = false, Message = "Người dùng đã là thành viên." };

            if (await _unitOfWork.GroupInvitationRepository.GetPendingInvitationsAsync(chatroomId, request.InvitedUserId) != null)
                return new ApiResponseDto { Success = false, Message = "Đã gửi lời mời trước đó." };

            var invitation = new GroupInvitation
            {
                InvitationId  = Guid.NewGuid(),
                ChatroomId    = chatroomId,
                InvitedUserId = request.InvitedUserId,
                InvitedBy     = userId,
                Message       = request.Message,
                Status        = "pending",
                SentAt        = DateTime.UtcNow,
                ExpiresAt     = DateTime.UtcNow.AddDays(7)
            };

            await _unitOfWork.GroupInvitations.AddAsync(invitation);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyGroupInvitationAsync(userId, request.InvitedUserId, chatroomId, invitation.InvitationId);

            var invitedConns = await _connectionManager.GetConnectionsAsync(request.InvitedUserId);
            if (invitedConns.Any())
            {
                var inviter = await _unitOfWork.Users.GetByIdAsync(userId);
                await _hubContext.Clients.Clients(invitedConns).SendAsync("GroupInvitationReceived", new
                {
                    InvitationId    = invitation.InvitationId,
                    ChatroomId      = chatroomId,
                    ChatroomName    = chatroom.RoomName,
                    InvitedBy       = userId,
                    InviterUsername = inviter?.Username,
                    Message         = request.Message,
                    ExpiresAt       = invitation.ExpiresAt
                });
            }

            return new ApiResponseDto { Success = true, Message = "Đã gửi lời mời." };
        }

        // ─────────────────────────────────────────────────────────────────────
        // ACCEPT
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> AcceptGroupInvitationAsync(Guid userId, Guid invitationId)
        {
            var invitation = await _unitOfWork.GroupInvitationRepository.GetInvitationForUserAsync(invitationId, userId)
                ?? throw new KeyNotFoundException("Không tìm thấy lời mời.");

            if (invitation.Status != "pending")
                return new ApiResponseDto { Success = false, Message = "Lời mời đã được xử lý." };

            if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt < DateTime.UtcNow)
                return new ApiResponseDto { Success = false, Message = "Lời mời đã hết hạn." };

            var alreadyMember = await _unitOfWork.ChatroomMemberRepository.HasActiveMemberAsync(invitation.ChatroomId, userId);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                invitation.Status      = "accepted";
                invitation.RespondedAt = DateTime.UtcNow;
                _unitOfWork.GroupInvitations.Update(invitation);

                if (!alreadyMember)
                {
                    var newMember = new ChatroomMember
                    {
                        MemberId   = Guid.NewGuid(),
                        ChatroomId = invitation.ChatroomId,
                        UserId     = userId,
                        MemberRole = "member",
                        AddedBy    = invitation.InvitedBy,
                        JoinedAt   = DateTime.UtcNow
                    };
                    await _unitOfWork.ChatroomMembers.AddAsync(newMember);
                    await _unitOfWork.MemberPermissionRepository.CreateDefaultAsync(newMember.MemberId, isAdmin: false);
                }

                await _unitOfWork.CommitTransactionAsync();

                await _hubContext.Clients.Group(invitation.ChatroomId.ToString())
                    .SendAsync("MemberJoined", new { ChatroomId = invitation.ChatroomId, UserId = userId });

                return new ApiResponseDto { Success = true, Message = "Đã tham gia phòng chat." };
            }
            catch { await _unitOfWork.RollbackTransactionAsync(); throw; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REJECT
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> RejectGroupInvitationAsync(Guid userId, Guid invitationId)
        {
            var invitation = await _unitOfWork.GroupInvitationRepository.GetInvitationForUserAsync(invitationId, userId)
                ?? throw new KeyNotFoundException("Không tìm thấy lời mời.");

            if (invitation.Status != "pending")
                return new ApiResponseDto { Success = false, Message = "Lời mời đã được xử lý." };

            invitation.Status      = "rejected";
            invitation.RespondedAt = DateTime.UtcNow;
            _unitOfWork.GroupInvitations.Update(invitation);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponseDto { Success = true, Message = "Đã từ chối lời mời." };
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<GroupInvitationRequest>> GetReceivedInvitationsAsync(Guid userId)
        {
            var invitations = await _unitOfWork.GroupInvitationRepository.GetReceivedPendingInvitationsAsync(userId);

            return invitations.Select(i => new GroupInvitationRequest
            {
                InvitationId      = i.InvitationId,
                ChatroomId        = i.ChatroomId,
                ChatroomName      = i.Chatroom?.RoomName      ?? string.Empty,
                ChatroomAvatar    = i.Chatroom?.Avatar         ?? string.Empty,
                InvitedByUserId   = i.InvitedBy,
                InvitedByUsername = i.InvitedByNavigation?.Username ?? string.Empty,
                InvitedByFullname = i.InvitedByNavigation?.Fullname ?? string.Empty,
                InvitedByAvatar   = i.InvitedByNavigation?.Avatar   ?? string.Empty,
                Message           = i.Message ?? string.Empty,
                Status            = i.Status,
                SentAt            = i.SentAt ?? DateTime.UtcNow,
                ExpiresAt         = i.ExpiresAt
            }).ToList();
        }
    }
}