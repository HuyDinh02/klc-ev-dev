using System;
using System.Threading.Tasks;
using KLC.Driver.Services;

namespace KLC.TestDoubles;

/// <summary>
/// Simple ICacheService that passes through to the factory (no caching in tests).
/// Shared across all BFF service tests to eliminate duplication.
/// </summary>
public class PassthroughCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) => Task.CompletedTask;
    public Task RemoveAsync(string key) => Task.CompletedTask;
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) => await factory();
}
