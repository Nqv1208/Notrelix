using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.RateLimiting;
using Notrelix.Infrastructure.RateLimiting;
using StackExchange.Redis;

namespace Notrelix.Infrastructure.Security.RateLimiting;

public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisRateLimitService _delegate;

    public RateLimitService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _delegate = new RedisRateLimitService(redis);
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

    public Task<RateLimitDecision> CheckAsync(
        RateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        return _delegate.CheckAsync(request, cancellationToken);
    }

    public Task<RateLimitDecision> CheckAsync(
        string policyName,
        string partitionKey,
        int permitLimit,
        TimeSpan window,
        RateLimitAlgorithm algorithm = RateLimitAlgorithm.SlidingWindow,
        CancellationToken cancellationToken = default)
    {
        return _delegate.CheckAsync(policyName, partitionKey, permitLimit, window, algorithm, cancellationToken);
    }

    private static string BuildKey(string action, string identifier)
        => $"Notrelix_ratelimit:{action}:{identifier.ToLowerInvariant()}";
}
