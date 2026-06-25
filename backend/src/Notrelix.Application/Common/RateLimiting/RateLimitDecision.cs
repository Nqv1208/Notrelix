namespace Notrelix.Application.Common.RateLimiting;

public sealed record RateLimitDecision(
    bool IsAllowed,
    int Limit,
    int Remaining,
    TimeSpan? RetryAfter,
    DateTimeOffset ResetAt);
