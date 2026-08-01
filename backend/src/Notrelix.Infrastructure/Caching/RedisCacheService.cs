using System.Text.Json;

namespace Notrelix.Infrastructure.Caching;

/// <summary>
/// Redis cache service with unified physical key construction.
/// All operations (Get/Set/Remove via IDistributedCache, Exists/Increment via IConnectionMultiplexer)
/// produce the same physical Redis key: "{InstanceName}{key}" where InstanceName = "Notrelix_".
/// </summary>
public class RedisCacheService : IRedisCacheService
{
    /// <summary>
    /// Must match CacheRegistration's AddStackExchangeRedisCache InstanceName.
    /// IDistributedCache prepends this automatically; direct IConnectionMultiplexer calls must prepend manually.
    /// </summary>
    internal const string KeyPrefix = "Notrelix_";

    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
    {
        _cache = cache;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await _cache.GetStringAsync(key, cancellationToken);
        if (data is null) return default;
        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"{KeyPrefix}{key}");
    }

    public async Task<long> IncrementAsync(string key, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var prefixedKey = $"{KeyPrefix}{key}";
        var value = await db.StringIncrementAsync(prefixedKey);

        if (value == 1 && expiration.HasValue)
        {
            await db.KeyExpireAsync(prefixedKey, expiration.Value);
        }

        return value;
    }
}
