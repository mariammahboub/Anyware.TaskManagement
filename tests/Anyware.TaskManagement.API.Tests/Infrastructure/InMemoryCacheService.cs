using System.Collections.Concurrent;
using System.Text.Json;
using Anyware.TaskManagement.Application.Common.Interfaces;

namespace Anyware.TaskManagement.API.Tests.Infrastructure;

public sealed class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, (string Json, DateTime? Expiry)> _store = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.Expiry.HasValue && entry.Expiry.Value < DateTime.UtcNow)
            {
                _store.TryRemove(key, out _);
                return Task.FromResult(default(T));
            }
            return Task.FromResult(JsonSerializer.Deserialize<T>(entry.Json, Options));
        }
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var json       = JsonSerializer.Serialize(value, Options);
        var expiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : (DateTime?)null;
        _store[key]    = (json, expiryTime);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public bool Contains(string key)
        => _store.TryGetValue(key, out var e)
           && (!e.Expiry.HasValue || e.Expiry.Value >= DateTime.UtcNow);
}
