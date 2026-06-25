namespace Notrelix.Application.Common.RateLimiting;

public sealed record RateLimitRequest(
    string PolicyName,
    string PartitionKey,
    int PermitLimit,
    TimeSpan Window,
    RateLimitAlgorithm Algorithm = RateLimitAlgorithm.SlidingWindow);

public enum RateLimitAlgorithm
{
    FixedWindow,
    SlidingWindow,
    TokenBucket,
}
