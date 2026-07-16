using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IMemberPermissionRepository: IRepository<MemberPermission>
    {
        /// Lấy permission theo userId và chatroomId
        Task<MemberPermission?> GetByUserAndChatroomAsync(Guid userId, Guid chatroomId, CancellationToken cancellationToken = default);
        /// Lấy permission theo memberId
        Task<MemberPermission?> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);
        /// Lấy tất cả permissions trong 1 chatroom
        Task<List<MemberPermission>> GetAllByChatroomAsync(Guid chatroomId, CancellationToken cancellationToken = default);
        /// Tạo permission mặc định khi user join chatroom
        Task<MemberPermission> CreateDefaultAsync(Guid memberId, bool isAdmin = false, CancellationToken cancellationToken = default);
        /// Cập nhật permission
        Task<MemberPermission> UpdateAsync(MemberPermission permission, CancellationToken cancellationToken = default);

        /// Xóa permission (khi member rời khỏi chatroom)
        Task<bool> DeleteAsync(Guid memberId, CancellationToken cancellationToken = default);

        /// Kiểm tra xem member có permission cụ thể không
        Task<bool> HasPermissionAsync(Guid userId, Guid chatroomId, PermissionType permissionName, CancellationToken cancellationToken = default);

        /// Lấy danh sách members có permission cụ thể
        Task<List<MemberPermission>> GetMembersWithPermissionAsync(Guid chatroomId, PermissionType permissionName, CancellationToken cancellationToken = default);
    }
}
