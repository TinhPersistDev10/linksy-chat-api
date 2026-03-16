using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Repositories.IRepositories;

namespace linksy_backend_api.Infrastructure.Services
{
    public class ChatroomAccessService : IChatroomAccessService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatroomAccessService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> IsMemberAsync(Guid chatroomId, Guid userId) =>
            await _unitOfWork.ChatroomMemberRepository
                .HasActiveMemberAsync(chatroomId, userId);

        public async Task<bool> IsAdminAsync(Guid chatroomId, Guid userId)
        {
            var member = await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberAsync(chatroomId, userId);
            return member?.MemberRole == "admin";
        }

        public async Task<bool> HasPermissionAsync(Guid chatroomId, Guid userId, PermissionType permissionType)
        {
            // Admin luôn có full quyền — không cần tra bảng permissions
            if (await IsAdminAsync(chatroomId, userId))
                return true;

            return await _unitOfWork.MemberPermissionRepository
                .HasPermissionAsync(userId, chatroomId, permissionType);
        }

        public async Task EnsureMemberAsync(Guid chatroomId, Guid userId)
        {
            if (!await IsMemberAsync(chatroomId, userId))
                throw new UnauthorizedAccessException(
                    "Bạn không phải thành viên của chatroom này");
        }

        public async Task EnsureAdminAsync(Guid chatroomId, Guid userId)
        {
            if (!await IsAdminAsync(chatroomId, userId))
                throw new UnauthorizedAccessException(
                    "Chỉ admin mới có quyền thực hiện hành động này");
        }

        public async Task EnsurePermissionAsync(Guid chatroomId, Guid userId, PermissionType permissionType)
        {
            if (!await HasPermissionAsync(chatroomId, userId, permissionType))
                throw new UnauthorizedAccessException(
                    $"Bạn không có quyền '{permissionType}' trong chatroom này");
        }

    }
}