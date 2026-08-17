using Notrelix.Application.Common.RateLimiting;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// Deterministic in-memory rate limiter for tests. FixedWindow semantics:
/// each CheckAsync call consumes one permit per (policy, partition) pair
/// and the decision reflects the remaining budget against the permit limit.
/// </summary>
public sealed class InMemoryRateLimitService : IRateLimitService
{
    private readonly Dictionary<string, int> _counts = new();

    public Task<bool> IsRateLimitedAsync(string action, string identifier, int maxAttempts, TimeSpan window)
    {
        var count = Increment(action, identifier);
        return Task.FromResult(count > maxAttempts);
    }

    public Task<int> GetRemainingAsync(string action, string identifier, int maxAttempts, TimeSpan window)
    {
        var count = GetCount(action, identifier);
        return Task.FromResult(Math.Max(0, maxAttempts - count));
    }

    public Task<RateLimitDecision> CheckAsync(
        RateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        return CheckAsync(
            request.PolicyName,
            request.PartitionKey,
            request.PermitLimit,
            request.Window,
            request.Algorithm,
            cancellationToken);
    }

    public Task<RateLimitDecision> CheckAsync(
        string policyName,
        string partitionKey,
        int permitLimit,
        TimeSpan window,
        RateLimitAlgorithm algorithm = RateLimitAlgorithm.SlidingWindow,
        CancellationToken cancellationToken = default)
    {
        var count = Increment(policyName, partitionKey);
        var allowed = count <= permitLimit;
        var remaining = Math.Max(0, permitLimit - count);
        return Task.FromResult(new RateLimitDecision(
            allowed, permitLimit, remaining,
            allowed ? null : window,
            DateTimeOffset.UtcNow.Add(window)));
    }

    private int Increment(string policyName, string partitionKey)
    {
        var key = $"{policyName}:{partitionKey}";
        var count = GetCount(policyName, partitionKey) + 1;
        _counts[key] = count;
        return count;
    }

    private int GetCount(string policyName, string partitionKey)
        => _counts.GetValueOrDefault($"{policyName}:{partitionKey}");
}
