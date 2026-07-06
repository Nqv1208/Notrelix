using Microsoft.Extensions.Options;
using Notrelix.API.Options;

namespace Notrelix.API.RateLimiting;

public sealed class RateLimitPolicyProvider : IRateLimitPolicyProvider
{
    private readonly RateLimitingOptions _options;

    public RateLimitPolicyProvider(IOptions<RateLimitingOptions> options)
    {
        _options = options.Value;
    }

    public RateLimitPolicy? GetPolicy(string policyName)
    {
        _options.Policies.TryGetValue(policyName, out var policy);
        return policy;
    }
}

public sealed class RateLimitPolicy
{
    public int PermitLimit { get; init; }
    public int WindowSeconds { get; init; }
    public string Algorithm { get; init; } = "SlidingWindow";
    public string FailMode { get; init; } = "Open";
    public PartitionKey PartitionBy { get; init; } = PartitionKey.Ip;
}
