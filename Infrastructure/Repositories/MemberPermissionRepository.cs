using linksy_backend_api.Domain.Entities.Models;
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

        public Task<List<MemberPermission>> GetMembersWithPermissionAsync(Guid chatroomId, string permissionName, CancellationToken cancellationToken = default)
        {
            var query = _context.MemberPermissions
                .Include(mp => mp.Member)
                    .ThenInclude(m => m.User)
                    .Where(mp => mp.Member.ChatroomId == chatroomId && mp.Member.LeftAt == null);
            query = permissionName switch
            {
                "CanSendMessages" => query.Where(mp => mp.CanSendMessages),
                "CanSendMedia" => query.Where(mp => mp.CanSendMedia),
                "CanSendVoice" => query.Where(mp => mp.CanSendVoice),
                "CanSendFiles" => query.Where(mp => mp.CanSendFiles),
                "CanInviteMembers" => query.Where(mp => mp.CanInviteMembers),
                "CanRemoveMembers" => query.Where(mp => mp.CanRemoveMembers),
                "CanEditGroupInfo" => query.Where(mp => mp.CanEditGroupInfo),
                "CanPinMessages" => query.Where(mp => mp.CanPinMessages),
                "CanDeleteMessages" => query.Where(mp => mp.CanDeleteMessages),
                "CanManageCalls" => query.Where(mp => mp.CanManageCalls),
                _ => throw new ArgumentException($"Permission '{permissionName}' không hợp lệ")
            };
            return query.ToListAsync(cancellationToken);


        }

        public async Task<bool> HasPermissionAsync(Guid userId, Guid chatroomId, string permissionName, CancellationToken cancellationToken = default)
        {

            var permission = await GetByUserAndChatroomAsync(userId, chatroomId, cancellationToken);
            if (permission == null) return false;

            return permissionName switch
            {
                "CanSendMessages" => permission.CanSendMessages,
                "CanSendMedia" => permission.CanSendMedia,
                "CanSendVoice" => permission.CanSendVoice,
                "CanSendFiles" => permission.CanSendFiles,
                "CanInviteMembers" => permission.CanInviteMembers,
                "CanRemoveMembers" => permission.CanRemoveMembers,
                "CanEditGroupInfo" => permission.CanEditGroupInfo,
                "CanPinMessages" => permission.CanPinMessages,
                "CanDeleteMessages" => permission.CanDeleteMessages,
                "CanManageCalls" => permission.CanManageCalls,
                _ => throw new ArgumentException($"Permission '{permissionName}' không hợp lệ")
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
