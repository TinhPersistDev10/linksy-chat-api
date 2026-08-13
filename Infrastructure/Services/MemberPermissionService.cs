// File: Infrastructure/Services/MemberPermissionService.cs

using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Requests.MemberPermission;
using linksy_backend_api.Domain.DTOs.Responses.MemberPermission;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Domain.Results;
using linksy_backend_api.Repositories.IRepositories;

namespace linksy_backend_api.Infrastructure.Services
{
    public class MemberPermissionService : IMemberPermissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MemberPermissionService> _logger;

        public MemberPermissionService(
            IUnitOfWork unitOfWork,
            ILogger<MemberPermissionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger     = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<MemberPermissionResponse>> GetAllAsync(Guid chatroomId)
        {
            var permissions = await _unitOfWork.MemberPermissionRepository
                .GetAllByChatroomAsync(chatroomId);

            return permissions.Select(p => MapToResponse(p, chatroomId)).ToList();
        }

        public async Task<MemberPermissionResponse> GetByUserAsync(Guid chatroomId, Guid userId)
        {
            var permission = await _unitOfWork.MemberPermissionRepository
                .GetByUserAndChatroomAsync(userId, chatroomId)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy permission record cho user {userId} trong chatroom {chatroomId}");

            return MapToResponse(permission, chatroomId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE (single)
        // ─────────────────────────────────────────────────────────────────────

        public async Task UpdateAsync(
            Guid chatroomId,
            Guid targetUserId,
            UpdateMemberPermissionRequest request)
        {
            var permission = await _unitOfWork.MemberPermissionRepository
                .GetByUserAndChatroomAsync(targetUserId, chatroomId)
                ?? throw new KeyNotFoundException(
                    $"Không tìm thấy permission record cho user {targetUserId}");

            ApplyUpdate(permission, request);

            await _unitOfWork.MemberPermissionRepository.UpdateAsync(permission);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Permissions updated for user {UserId} in chatroom {ChatroomId}",
                targetUserId, chatroomId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // BATCH UPDATE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<BatchUpdateResult> BatchUpdateAsync(
            Guid chatroomId,
            Guid callerId,
            BatchUpdatePermissionRequest request)
        {
            if (!request.Permissions.Any())
                return BatchUpdateResult.Empty();

            var updatedCount = 0;
            var errors       = new List<string>();

            foreach (var item in request.Permissions)
            {
                if (item.UserId == callerId)
                {
                    errors.Add($"Bỏ qua {item.UserId}: admin không thể tự thay đổi quyền của mình");
                    continue;
                }

                var permission = await _unitOfWork.MemberPermissionRepository
                    .GetByUserAndChatroomAsync(item.UserId, chatroomId);

                if (permission is null)
                {
                    errors.Add($"Bỏ qua {item.UserId}: không tìm thấy permission record");
                    continue;
                }

                ApplyUpdate(permission, item.Permissions);
                await _unitOfWork.MemberPermissionRepository.UpdateAsync(permission);
                updatedCount++;
            }

            // Một lần SaveChanges duy nhất cho toàn bộ batch
            if (updatedCount > 0)
                await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Batch update in chatroom {ChatroomId}: {Count} updated, {ErrorCount} skipped",
                chatroomId, updatedCount, errors.Count);

            return BatchUpdateResult.Success(updatedCount, errors);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MUTE / UNMUTE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<MuteResult> MuteMemberAsync(
            Guid chatroomId,
            Guid targetUserId,
            MuteMemberRequest request)
        {
            var member = await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberAsync(chatroomId, targetUserId)
                ?? throw new KeyNotFoundException("Không tìm thấy thành viên trong chatroom");

            member.IsMuted    = true;
            member.MutedUntil = request.DurationInMinutes.HasValue
                ? DateTime.UtcNow.AddMinutes(request.DurationInMinutes.Value)
                : null;

            _unitOfWork.ChatroomMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} muted in chatroom {ChatroomId}, until: {Until}",
                targetUserId, chatroomId, member.MutedUntil?.ToString("u") ?? "indefinite");

            return MuteResult.Success(member.IsMuted ?? true, member.MutedUntil);
        }

        public async Task UnmuteMemberAsync(Guid chatroomId, Guid targetUserId)
        {
            var member = await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberAsync(chatroomId, targetUserId)
                ?? throw new KeyNotFoundException("Không tìm thấy thành viên trong chatroom");

            member.IsMuted    = false;
            member.MutedUntil = null;

            _unitOfWork.ChatroomMembers.Update(member);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} unmuted in chatroom {ChatroomId}",
                targetUserId, chatroomId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Áp dụng từng field nullable từ request lên entity.
        /// Chỉ ghi đè khi caller thực sự truyền giá trị.
        /// Dùng PermissionTypes constants để đảm bảo tên field nhất quán.
        /// </summary>
        private static void ApplyUpdate(
            MemberPermission permission,
            UpdateMemberPermissionRequest request)
        {
            if (request.CanSendMessages  is not null) permission.CanSendMessages  = request.CanSendMessages.Value;
            if (request.CanSendMedia     is not null) permission.CanSendMedia     = request.CanSendMedia.Value;
            if (request.CanSendVoice     is not null) permission.CanSendVoice     = request.CanSendVoice.Value;
            if (request.CanSendFiles     is not null) permission.CanSendFiles     = request.CanSendFiles.Value;
            if (request.CanInviteMembers is not null) permission.CanInviteMembers = request.CanInviteMembers.Value;
            if (request.CanRemoveMembers is not null) permission.CanRemoveMembers = request.CanRemoveMembers.Value;
            if (request.CanEditGroupInfo is not null) permission.CanEditGroupInfo = request.CanEditGroupInfo.Value;
            if (request.CanPinMessages   is not null) permission.CanPinMessages   = request.CanPinMessages.Value;
            if (request.CanDeleteMessages is not null) permission.CanDeleteMessages = request.CanDeleteMessages.Value;
            if (request.CanManageCalls   is not null) permission.CanManageCalls   = request.CanManageCalls.Value;
        }

        /// <summary>
        /// Map entity → response DTO.
        /// Tập trung mapping ở một chỗ duy nhất.
        /// </summary>
        private static MemberPermissionResponse MapToResponse(
            MemberPermission p,
            Guid chatroomId)
        {
            return new MemberPermissionResponse
            {
                PermissionId      = p.PermissionId,
                MemberId          = p.MemberId,
                UserId            = p.Member.UserId,
                UserName          = p.Member.User.Username,
                UserAvatar        = DefaultAvatarHelper.GetAvatarOrDefault(
                                        p.Member.User.Avatar,
                                        p.Member.UserId,
                                        username: p.Member.User.Username,
                                        fullname: p.Member.User.Fullname),
                Email             = p.Member.User.Email,
                ChatroomId        = chatroomId,
                ChatroomName      = p.Member.Chatroom?.RoomName ?? string.Empty,
                CanSendMessages   = p.CanSendMessages,
                CanSendMedia      = p.CanSendMedia,
                CanSendVoice      = p.CanSendVoice,
                CanSendFiles      = p.CanSendFiles,
                CanDeleteMessages = p.CanDeleteMessages,
                CanPinMessages    = p.CanPinMessages,
                CanInviteMembers  = p.CanInviteMembers,
                CanRemoveMembers  = p.CanRemoveMembers,
                CanEditGroupInfo  = p.CanEditGroupInfo,
                CanManageCalls    = p.CanManageCalls,
                CreatedAt         = p.CreatedAt,
                UpdatedAt         = p.UpdatedAt
            };
        }
    }
}