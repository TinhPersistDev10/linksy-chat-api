using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.Enums;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IChatroomAccessService
    {
        /// <summary>Kiểm tra user có đang là active member của chatroom không.</summary>
        Task<bool> IsMemberAsync(Guid chatroomId, Guid userId);

        /// <summary>Kiểm tra user có role "admin" trong chatroom không.</summary>
        Task<bool> IsAdminAsync(Guid chatroomId, Guid userId);

        /// <summary>
        /// Kiểm tra user có quyền cụ thể không.
        /// Admin luôn có tất cả các quyền — không cần kiểm tra permission record.
        /// </summary>
        Task<bool> HasPermissionAsync(Guid chatroomId, Guid userId, PermissionType permissionType);

        /// <summary>
        /// Đảm bảo user là member — throw UnauthorizedAccessException nếu không.
        /// Dùng thay cho pattern if(!isMember) throw ... lặp đi lặp lại.
        /// </summary>
        Task EnsureMemberAsync(Guid chatroomId, Guid userId);

        /// <summary>
        /// Đảm bảo user là admin — throw UnauthorizedAccessException nếu không.
        /// </summary>
        Task EnsureAdminAsync(Guid chatroomId, Guid userId);

        /// <summary>
        /// Đảm bảo user có quyền cụ thể — throw UnauthorizedAccessException nếu không.
        /// </summary>
        Task EnsurePermissionAsync(Guid chatroomId, Guid userId, PermissionType permissionType);
    }
}