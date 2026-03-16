using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.DTOs.Requests.MemberPermission;
using linksy_backend_api.Domain.DTOs.Responses.MemberPermission;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Infrastructure.Services.Results;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface IMemberPermissionService
    {
        // ── GET ──────────────────────────────────────────────────────────────

        /// <summary>Lấy toàn bộ permissions của tất cả member trong chatroom.</summary>
        Task<List<MemberPermissionResponse>> GetAllAsync(Guid chatroomId);

        /// <summary>Lấy permission của một user cụ thể trong chatroom.</summary>
        Task<MemberPermissionResponse> GetByUserAsync(Guid chatroomId, Guid userId);

        // ── UPDATE ───────────────────────────────────────────────────────────

        /// <summary>Cập nhật permission của một member (admin only).</summary>
        Task UpdateAsync(
            Guid chatroomId,
            Guid targetUserId,
            UpdateMemberPermissionRequest request);

        /// <summary>
        /// Batch update permissions cho nhiều members cùng lúc.
        /// Trả về <see cref="BatchUpdateResult"/> gồm số lượng thành công và danh sách lỗi.
        /// </summary>
        Task<BatchUpdateResult> BatchUpdateAsync(
            Guid chatroomId,
            Guid callerId,
            BatchUpdatePermissionRequest request);

        // ── MUTE ─────────────────────────────────────────────────────────────

        /// <summary>Mute một member trong chatroom.</summary>
        Task<MuteResult> MuteMemberAsync(
            Guid chatroomId,
            Guid targetUserId,
            MuteMemberRequest request);

        /// <summary>Unmute một member trong chatroom.</summary>
        Task UnmuteMemberAsync(Guid chatroomId, Guid targetUserId);
    }
}