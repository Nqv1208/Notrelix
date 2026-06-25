using Notrelix.API.Options;

namespace Notrelix.API.RateLimiting;

public interface IRateLimitPolicyProvider
{
    RateLimitPolicy? GetPolicy(string policyName);
}
