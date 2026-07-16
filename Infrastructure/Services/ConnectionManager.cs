using System.Collections.Concurrent;
using linksy_backend_api.Services.IServices;

namespace linksy_backend_api.Services
{
    public class ConnectionManager : IConnectionManager
    {
        private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _userConnections
            = new();

        private static readonly ConcurrentDictionary<string, Guid> _connectionUsers = new();
        public Task AddConnectionAsync(Guid userId, string connectionId)
        {
            // Add to user -> connections mapping
            var connections = _userConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            connections.TryAdd(connectionId, default);
            _connectionUsers[connectionId] = userId;
            return Task.CompletedTask;
        }

        public Task<List<string>> GetConnectionsAsync(Guid userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                return Task.FromResult(connections.Keys.ToList());
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
            return _connectionUsers.TryGetValue(connectionId, out var userId)
                ? Task.FromResult<Guid?> (userId)
                : Task.FromResult<Guid?> (null);
        }

        public Task<bool> HasConnectionsAsync(Guid userId)
        {
            return _userConnections.TryGetValue(userId, out var connections) ? Task.FromResult(!connections.IsEmpty) : Task.FromResult(false);
        }

        public Task RemoveConnectionAsync(Guid userId, string connectionId)
        {
            _connectionUsers.TryRemove(connectionId, out _);
            if (!_userConnections.TryGetValue(userId, out var connections))
                return Task.CompletedTask;

            connections.TryRemove(connectionId, out _);
            if (connections.IsEmpty) _userConnections.TryRemove(userId, out _);

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
            return HasConnectionsAsync(userId);
        }
    }
}