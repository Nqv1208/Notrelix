using Notrelix.API.RateLimiting;

namespace Notrelix.API.Options;

public sealed class RateLimitingOptions
{
    public Dictionary<string, RateLimitPolicy> Policies { get; init; } = new();
}
