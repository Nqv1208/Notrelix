using System.Text.Json;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Caching;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
    {
        _cache = cache;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);
        if (data is null) return default;
        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"Notrelix_{key}");
    }

    public async Task<long> IncrementAsync(string key, TimeSpan? expiration = null)
    {
        var db = _redis.GetDatabase();
        var prefixedKey = $"Notrelix_{key}";
        var value = await db.StringIncrementAsync(prefixedKey);

        if (value == 1 && expiration.HasValue)
        {
            await db.KeyExpireAsync(prefixedKey, expiration.Value);
        }

        return value;
    }
}
