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
            return await Query()
            .Where(n => n.UserId == userId &&
            (n.IsRead == null || n.IsRead == false) &&
            (n.IsDeleted == null || n.IsDeleted == false))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int page, int pageSize)
        {
            return await Query()
                    .Where(n => n.UserId == userId && (n.IsDeleted == null || n.IsDeleted == false))
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var unreadNotifications = await Query()
                 .Where(n => n.UserId == userId &&
                            (n.IsRead == null || n.IsRead == false) &&
                            (n.IsDeleted == null || n.IsDeleted == false))
                 .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                // UpdateRange không cần await vì nó chỉ track changes
                UpdateRange(unreadNotifications);
                // SaveChangesAsync sẽ được gọi từ Service layer hoặc UnitOfWork
            }
        }

    }
}