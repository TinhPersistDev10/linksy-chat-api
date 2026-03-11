using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface INotificationRepository : IRepository<Notification>
    {
         Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int page, int pageSize);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAllAsReadAsync(Guid userId);
        Task<List<Notification>> GetUnreadNotificationsAsync(Guid userId);
        Task<int> GetTotalCountAsync(Guid userId);
        
    }
}