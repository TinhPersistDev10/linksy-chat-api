// Infrastructure/Services/CacheService.cs
using System.Text.Json;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace linksy_backend_api.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly bool _enabled;
        private readonly TimeSpan _defaultExpiry;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public CacheService(
            IDistributedCache cache,
            IConfiguration configuration,
            ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _enabled = configuration.GetValue<bool>("Redis:Enabled", true);
            _defaultExpiry = TimeSpan.FromSeconds(
                configuration.GetValue<int>("Redis:DefaultExpiry", 300));
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (!_enabled) return null;

            try
            {
                var data = await _cache.GetStringAsync(key);
                if (data is null) return null;

                return JsonSerializer.Deserialize<T>(data, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GET failed for key: {Key}", key);
                return null; // Cache miss — fall through to DB
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            if (!_enabled) return;

            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry ?? _defaultExpiry
                };
                var data = JsonSerializer.Serialize(value, _jsonOptions);
                await _cache.SetStringAsync(key, data, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis SET failed for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (!_enabled) return;

            try { await _cache.RemoveAsync(key); }
            catch (Exception ex) { _logger.LogWarning(ex, "Redis REMOVE failed for key: {Key}", key); }
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            if (!_enabled) return;

            // StackExchange.Redis không hỗ trợ wildcard delete qua IDistributedCache
            // Cần inject IConnectionMultiplexer trực tiếp cho feature này
            _logger.LogDebug("RemoveByPrefix called for prefix: {Prefix}", prefix);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            if (!_enabled) return false;

            try
            {
                var data = await _cache.GetStringAsync(key);
                return data is not null;
            }
            catch { return false; }
        }

        public async Task<T> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiry = null) where T : class
        {
            var cached = await GetAsync<T>(key);
            if (cached is not null) return cached;

            var value = await factory();
            await SetAsync(key, value, expiry);
            return value;
        }
    }
}