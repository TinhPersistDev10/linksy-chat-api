using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        private readonly LinksyDbContext _context;
        public NotificationRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> GetTotalCountAsync(Guid userId)
        {
            return await Query()
                .Where(n => n.UserId == userId && (n.IsDeleted == null || n.IsDeleted == false))
                .CountAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await Query()
                .Where(n => n.UserId == userId &&
                (n.IsRead == null || n.IsRead == false) &&
                (n.IsDeleted == null || n.IsDeleted == false))
                .CountAsync();
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId)
        {
            return await ActiveQuery(userId)
            .Where(n => n.IsRead != true)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            return await ActiveQuery(userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await ActiveQuery(userId)
            .Where(n => n.IsRead != true)
            .ExecuteUpdateAsync(s => s
            .SetProperty(n => n.IsRead, true)
            .SetProperty(n => n.ReadAt, DateTime.UtcNow));
        }

        public async Task SoftDeleteAllReadSync(Guid userId)
        {
            await ActiveQuery(userId)
            .Where(n => n.IsRead == true)
            .ExecuteUpdateAsync(s => s
            .SetProperty(n => n.IsDeleted, true)
            .SetProperty(n => n.DeletedAt, DateTime.UtcNow));
        }

        public async Task SoftDeleteAllSync(Guid userId)
        {
            await ActiveQuery(userId)
            .ExecuteUpdateAsync(s => s
            .SetProperty(n => n.IsDeleted, true)
            .SetProperty(n => n.DeletedAt, DateTime.UtcNow));
        }

        public async Task SoftDeleteByIdsAsync(Guid userId, List<Guid> ids)
        {
            await ActiveQuery(userId)
            .Where(n=>ids.Contains(n.NotificationId))
            .ExecuteUpdateAsync(s => s
            .SetProperty(n => n.IsDeleted, true)
            .SetProperty(n => n.DeletedAt, DateTime.UtcNow));
        }

        public async Task SoftDeleteOlderThanAsync(Guid userId, int days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            await ActiveQuery(userId)
            .Where(n => n.CreatedAt < cutoff)
            .ExecuteUpdateAsync(s => s
            .SetProperty(n => n.IsDeleted, true)
            .SetProperty(n => n.DeletedAt, DateTime.UtcNow));
        }
        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        // Base query dùng chung — tránh lặp điều kiện IsDeleted ở khắp nơi
        private IQueryable<Notification> ActiveQuery(Guid userId) =>
            Query().Where(n => n.UserId == userId && n.IsDeleted != true);
    }

}