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

    private const string IncrementWithConditionalDeleteScript = """
        local attempts = redis.call('INCR', KEYS[1])
        if attempts == 1 and tonumber(ARGV[1]) > 0 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        local exceeded = 0
        if attempts > tonumber(ARGV[2]) then
            redis.call('DEL', KEYS[2])
            exceeded = 1
        end
        return {attempts, exceeded}
        """;

    /// <summary>
    /// Atomically increments a counter and deletes another key when the
    /// counter exceeds <paramref name="max"/>. Used for cumulative attempt
    /// budgets that must not reset on wall-clock windows.
    /// Returns the new counter value and whether the delete was performed.
    /// </summary>
    public async Task<(long Attempts, bool Exceeded)> IncrementWithConditionalDeleteAsync(
        string incrementKey, string deleteKey, long max, TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var result = await db.ScriptEvaluateAsync(
            IncrementWithConditionalDeleteScript,
            new RedisKey[] { $"{KeyPrefix}{incrementKey}", $"{KeyPrefix}{deleteKey}" },
            new RedisValue[] { ttl?.TotalSeconds ?? 0, max });

        var attempts = (long)result[0];
        var exceeded = (long)result[1] == 1;
        return (attempts, exceeded);
    }

    private const string GetDeleteScript = """
        local value = redis.call('HGET', KEYS[1], 'data')
        if value == false then
            return nil
        end
        redis.call('DEL', KEYS[1])
        return value
        """;

    public async Task<T?> GetDeleteAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Microsoft RedisCache stores entries as hashes (absexp/sldexp/data fields), so the
        // consume reads field 'data' then deletes the whole entry in one atomic Lua step.
        var db = _redis.GetDatabase();
        var result = await db.ScriptEvaluateAsync(GetDeleteScript, new RedisKey[] { $"{KeyPrefix}{key}" });
        if (result.IsNull)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>((string)result!);
    }
}
