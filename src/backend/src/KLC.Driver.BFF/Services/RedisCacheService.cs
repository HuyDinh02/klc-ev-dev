using System.Collections.Concurrent;
using System.Text.Json;
using StackExchange.Redis;

namespace KLC.Driver.Services;

/// <summary>
/// Redis cache service implementation for cache-first pattern.
/// Cache key format: "entity:{id}:field" (e.g., "station:123:status")
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);
    // Per-key locks to prevent thundering herd: multiple concurrent misses for the same
    // key will queue behind a single factory call rather than all hitting the DB.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache get failed for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiration ?? _defaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache set failed for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache remove failed for key: {Key}", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        // Fast path: cache hit without acquiring any lock
        var cached = await GetAsync<T>(key);
        if (cached is not null)
        {
            return cached;
        }

        // Slow path: serialize concurrent misses for the same key to avoid thundering herd
        var sem = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            // Re-check after acquiring lock — a prior waiter may have populated it
            cached = await GetAsync<T>(key);
            if (cached is not null)
            {
                return cached;
            }

            var value = await factory();
            if (value is not null)
            {
                await SetAsync(key, value, expiration);
            }
            return value;
        }
        finally
        {
            sem.Release();
            _keyLocks.TryRemove(key, out _);
        }
    }
}
