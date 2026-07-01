using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.RateLimiting;

namespace Notrelix.Infrastructure.RateLimiting;

public sealed class RedisRateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimitService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<RateLimitDecision> CheckAsync(
        RateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CheckAsync(
            request.PolicyName,
            request.PartitionKey,
            request.PermitLimit,
            request.Window,
            request.Algorithm,
            cancellationToken);
    }

    public async Task<RateLimitDecision> CheckAsync(
        string policyName,
        string partitionKey,
        int permitLimit,
        TimeSpan window,
        RateLimitAlgorithm algorithm = RateLimitAlgorithm.SlidingWindow,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(policyName, partitionKey);
        var now = DateTimeOffset.UtcNow;
        var windowSeconds = (long)window.TotalSeconds;

        if (algorithm == RateLimitAlgorithm.FixedWindow)
        {
            var windowKey = $"{key}:{(now.ToUnixTimeSeconds() / windowSeconds)}";
            var fwCount = await db.StringIncrementAsync(windowKey);
            if (fwCount == 1)
                await db.KeyExpireAsync(windowKey, window);
            var fwRemaining = Math.Max(0, permitLimit - (int)fwCount);
            var fwResetAt = new DateTimeOffset(
                (now.ToUnixTimeSeconds() / windowSeconds + 1) * windowSeconds,
                TimeSpan.Zero);
            return new RateLimitDecision(
                fwCount <= permitLimit, permitLimit, fwRemaining,
                fwCount > permitLimit ? fwResetAt - now : null, fwResetAt);
        }

        var swScore = now.ToUnixTimeMilliseconds();
        var swMinScore = swScore - windowSeconds * 1000;
        var member = $"{swScore}:{Guid.NewGuid():N}";

        var tran = db.CreateTransaction();
        _ = tran.SortedSetRemoveRangeByScoreAsync(key, 0, swMinScore);
        _ = tran.SortedSetAddAsync(key, member, swScore);
        var countTask = tran.SortedSetLengthAsync(key);
        _ = tran.KeyExpireAsync(key, window);
        await tran.ExecuteAsync();

        var swTotal = (int)await countTask;
        var swRemaining = Math.Max(0, permitLimit - swTotal);
        var swResetAt = now.Add(window);
        return new RateLimitDecision(
            swTotal <= permitLimit, permitLimit, swRemaining,
            swTotal > permitLimit ? window : null, swResetAt);
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

    private static string BuildKey(string policy, string partition)
        => $"Notrelix_ratelimit:{policy}:{partition.ToLowerInvariant()}";
}
