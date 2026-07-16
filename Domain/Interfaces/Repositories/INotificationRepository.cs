using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface INotificationRepository : IRepository<Notification>
    {
        //Queries
        Task<int> GetTotalCountAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId);
        Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int page, int pageSize);

        //Bulk Operations
        Task MarkAllAsReadAsync(Guid userId);
        Task SoftDeleteAllSync(Guid userId);
        Task SoftDeleteAllReadSync(Guid userId);
        Task SoftDeleteByIdsAsync(Guid userId, List<Guid> ids);
        Task SoftDeleteOlderThanAsync(Guid userId, int days);

    }
}