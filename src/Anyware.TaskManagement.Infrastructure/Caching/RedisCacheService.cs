using Anyware.TaskManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure.Caching
{
    internal sealed class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

        public RedisCacheService(
            IConnectionMultiplexer redis,
            ILogger<RedisCacheService> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }


        public async Task<T?> GetAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _database.StringGetAsync(key);

                if (!value.HasValue)
                {
                    _logger.LogDebug("Cache MISS for key '{Key}'", key);
                    return default;
                }

                _logger.LogDebug("Cache HIT for key '{Key}'", key);
                return JsonSerializer.Deserialize<T>(value!, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis GET failed for key '{Key}'. Falling back to database.", key);
                return default;
            }
        }

        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, JsonOptions);
                var resolvedExp = expiry ?? DefaultExpiry;

                await _database.StringSetAsync(key, json, resolvedExp);

                _logger.LogDebug(
                    "Cache SET for key '{Key}' with expiry {Expiry}",
                    key, resolvedExp);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis SET failed for key '{Key}'. Data will not be cached.", key);
            }
        }
        public async Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
                _logger.LogDebug("Cache INVALIDATED for key '{Key}'", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis DELETE failed for key '{Key}'. Cache may be stale.", key);
            }
        }
    }
}
