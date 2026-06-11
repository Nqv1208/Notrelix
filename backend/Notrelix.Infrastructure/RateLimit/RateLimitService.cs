using StackExchange.Redis;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.RateLimit;

public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;

    public RateLimitService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> IsRateLimitedAsync(string action, string identifier, int maxAttempts, TimeSpan window)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(action, identifier);

        var count = await db.StringIncrementAsync(key);
        if (count == 1)
            await db.KeyExpireAsync(key, window);

        return count > maxAttempts;
    }

    public async Task<int> GetRemainingAsync(string action, string identifier, int maxAttempts, TimeSpan window)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(action, identifier);
        var value = await db.StringGetAsync(key);

        if (!value.HasValue) return maxAttempts;

        var used = (int)value;
        return Math.Max(0, maxAttempts - used);
    }

    private static string BuildKey(string action, string identifier)
        => $"Notrelix_ratelimit:{action}:{identifier.ToLowerInvariant()}";
}
