using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Services.IServices
{
    public interface IConnectionManager
    {
        Task AddConnectionAsync(Guid userId, string connectionId);
        Task RemoveConnectionAsync(Guid userId, string connectionId);
        Task<List<string>> GetConnectionsAsync(Guid userId);
        Task<bool> HasConnectionsAsync(Guid userId);
        Task<Guid?> GetUserIdByConnectionAsync(string connectionId);
        Task<Dictionary<Guid, int>> GetOnlineUsersCountAsync();

        Task<List<Guid>> GetOnlineUsersAsync();
        Task<bool> IsUserOnlineAsync(Guid userId);
    }
}