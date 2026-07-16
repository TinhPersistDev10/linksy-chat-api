using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Repositories
{
    public class MemberPermissionRepository : Repository<MemberPermission>, IMemberPermissionRepository
    {

        private readonly LinksyDbContext _context;
        //private readonly ILogger<MemberPermissionRepository> _logger;
        public MemberPermissionRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<MemberPermission> CreateDefaultAsync(Guid memberId, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            var permission = new MemberPermission
            {
                PermissionId = Guid.NewGuid(),
                MemberId = memberId,
                CanSendMessages = true,
                CanSendMedia = true,
                CanSendVoice = true,
                CanSendFiles = true,
                CanInviteMembers = isAdmin,
                CanRemoveMembers = isAdmin,
                CanEditGroupInfo = isAdmin,
                CanPinMessages = isAdmin,
                CanDeleteMessages = true,
                CanManageCalls = isAdmin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.MemberPermissions.AddAsync(permission, cancellationToken);
            return permission;
        }

        public async Task<bool> DeleteAsync(Guid memberId, CancellationToken cancellationToken = default)
        {
            var permission = await _context.MemberPermissions.FirstOrDefaultAsync(mp => mp.MemberId == memberId, cancellationToken);
            if (permission == null)
                return false;
            _context.MemberPermissions.Remove(permission);
            return true;
        }

        public async Task<List<MemberPermission>> GetAllByChatroomAsync(Guid chatroomId, CancellationToken cancellationToken = default)
        {
            return await _context.MemberPermissions
                .Include(mp => mp.Member)
                    .ThenInclude(m => m.User)
                .Where(mp => mp.Member.ChatroomId == chatroomId)
                .ToListAsync(cancellationToken);
        }

        public async Task<MemberPermission?> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
        {
            return await _context.MemberPermissions
                .FirstOrDefaultAsync(mp => mp.MemberId == memberId, cancellationToken);
        }

        public async Task<MemberPermission?> GetByUserAndChatroomAsync(Guid userId, Guid chatroomId, CancellationToken cancellationToken = default)
        {
            return await _context.MemberPermissions
                .Include(mp => mp.Member)
                .ThenInclude(m => m.User)
                .Include(mp => mp.Member)
                .ThenInclude(m => m.Chatroom)
                .FirstOrDefaultAsync(
                mp => mp.Member.UserId == userId && mp.Member.ChatroomId == chatroomId,
                cancellationToken);
        }

        public Task<List<MemberPermission>> GetMembersWithPermissionAsync(
    Guid chatroomId,
    PermissionType permission,       // ← enum
    CancellationToken cancellationToken = default)
        {
            var query = _context.MemberPermissions
                .Include(mp => mp.Member).ThenInclude(m => m.User)
                .Where(mp => mp.Member.ChatroomId == chatroomId && mp.Member.LeftAt == null);

            query = permission switch
            {
                PermissionType.CanSendMessages => query.Where(mp => mp.CanSendMessages),
                PermissionType.CanSendMedia => query.Where(mp => mp.CanSendMedia),
                PermissionType.CanSendVoice => query.Where(mp => mp.CanSendVoice),
                PermissionType.CanSendFiles => query.Where(mp => mp.CanSendFiles),
                PermissionType.CanInviteMembers => query.Where(mp => mp.CanInviteMembers),
                PermissionType.CanRemoveMembers => query.Where(mp => mp.CanRemoveMembers),
                PermissionType.CanEditGroupInfo => query.Where(mp => mp.CanEditGroupInfo),
                PermissionType.CanPinMessages => query.Where(mp => mp.CanPinMessages),
                PermissionType.CanDeleteMessages => query.Where(mp => mp.CanDeleteMessages),
                PermissionType.CanManageCalls => query.Where(mp => mp.CanManageCalls),
                _ => query
            };
            return query.ToListAsync(cancellationToken);
        }

        public async Task<bool> HasPermissionAsync(
    Guid userId,
    Guid chatroomId,
    PermissionType permission,       // ← enum, không phải string
    CancellationToken cancellationToken = default)
        {
            var record = await GetByUserAndChatroomAsync(userId, chatroomId, cancellationToken);
            if (record is null) return false;

            // Không còn default throw — compiler đảm bảo mọi case đều được xử lý
            return permission switch
            {
                PermissionType.CanSendMessages => record.CanSendMessages,
                PermissionType.CanSendMedia => record.CanSendMedia,
                PermissionType.CanSendVoice => record.CanSendVoice,
                PermissionType.CanSendFiles => record.CanSendFiles,
                PermissionType.CanInviteMembers => record.CanInviteMembers,
                PermissionType.CanRemoveMembers => record.CanRemoveMembers,
                PermissionType.CanEditGroupInfo => record.CanEditGroupInfo,
                PermissionType.CanPinMessages => record.CanPinMessages,
                PermissionType.CanDeleteMessages => record.CanDeleteMessages,
                PermissionType.CanManageCalls => record.CanManageCalls,
                _ => false
            };
        }

        public async Task<MemberPermission> UpdateAsync(MemberPermission permission, CancellationToken cancellationToken = default)
        {
            permission.UpdatedAt = DateTime.UtcNow;
            _context.MemberPermissions.Update(permission);
            return permission;
        }
    }
}
