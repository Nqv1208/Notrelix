namespace Notrelix.Application.Common.RateLimiting;

public interface IRateLimitService
{
    Task<bool> IsRateLimitedAsync(string action, string identifier, int maxAttempts, TimeSpan window);
    Task<int> GetRemainingAsync(string action, string identifier, int maxAttempts, TimeSpan window);

    Task<RateLimitDecision> CheckAsync(
        RateLimitRequest request,
        CancellationToken cancellationToken = default);

    Task<RateLimitDecision> CheckAsync(
        string policyName,
        string partitionKey,
        int permitLimit,
        TimeSpan window,
        RateLimitAlgorithm algorithm = RateLimitAlgorithm.SlidingWindow,
        CancellationToken cancellationToken = default);
}
