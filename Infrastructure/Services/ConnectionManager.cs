using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Services.IServices;

namespace linksy_backend_api.Services
{
    public class ConnectionManager : IConnectionManager
    {
        // Dictionary: UserId -> List of ConnectionIds
        private static readonly ConcurrentDictionary<Guid, HashSet<string>> _userConnections = new ConcurrentDictionary<Guid, HashSet<string>>();

        // Dictionary: ConnectionId -> UserId
        private static readonly ConcurrentDictionary<string, Guid> _connectionUsers = new ConcurrentDictionary<string, Guid>();
        public Task AddConnectionAsync(Guid userId, string connectionId)
        {
            // Add to user -> connections mapping
            _userConnections.AddOrUpdate(userId, new HashSet<string> { connectionId }, (key, oldValue) =>
            {
                oldValue.Add(connectionId);
                return oldValue;
            });

            // Add to connection -> user mapping
            _connectionUsers.TryAdd(connectionId, userId);

            return Task.CompletedTask;
        }

        public Task<List<string>> GetConnectionsAsync(Guid userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                    return Task.FromResult(connections.ToList());
            }
            return Task.FromResult(new List<string>());
        }

        public Task<Dictionary<Guid, int>> GetOnlineUsersCountAsync()
        {
            var result = _userConnections.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            );

            return Task.FromResult(result);
        }

        public Task<Guid?> GetUserIdByConnectionAsync(string connectionId)
        {
            if (_connectionUsers.TryGetValue(connectionId, out var userId))
            {
                return Task.FromResult<Guid?>(userId);
            }
            return Task.FromResult<Guid?>(null);
        }

        public Task<bool> HasConnectionsAsync(Guid userId)
        {
            return Task.FromResult(_userConnections.ContainsKey(userId));
        }

        public Task RemoveConnectionAsync(Guid userId, string connectionId)
        {
            // Remove from user -> connections mapping
            _userConnections.AddOrUpdate(userId, new HashSet<string>(), (key, oldValue) =>
            {
                oldValue.Remove(connectionId);
                return oldValue;
            });

            // Remove from connection -> user mapping
            _connectionUsers.TryRemove(connectionId, out _);

            return Task.CompletedTask;
        }
        // Helper method để lấy danh sách tất cả users online
        public Task<List<Guid>> GetOnlineUsersAsync()
        {
            return Task.FromResult(_userConnections.Keys.ToList());
        }
        // Helper method để kiểm tra user có online không
        public Task<bool> IsUserOnlineAsync(Guid userId)
        {
            return Task.FromResult(_userConnections.ContainsKey(userId));
        }
    }
}