namespace Notrelix.Infrastructure.RateLimiting;

public sealed class RedisRateLimitOptions
{
    public string InstanceId { get; init; } = "default";
}
