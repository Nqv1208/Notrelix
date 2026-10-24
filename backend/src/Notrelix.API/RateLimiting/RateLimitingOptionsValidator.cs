using Microsoft.Extensions.Options;
using Notrelix.API.Options;

namespace Notrelix.API.RateLimiting;

/// <summary>
/// Validates the configured rate-limit policies at startup and finds policy names declared by an
/// endpoint through <see cref="RateLimitPolicyAttribute"/> that resolve to no configured policy.
/// The endpoint check runs in the API test suite rather than at host startup: enumerating every
/// endpoint eagerly costs seconds of startup per host.
/// </summary>
public sealed class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
{
    private static readonly string[] Algorithms = ["SlidingWindow", "FixedWindow", "TokenBucket"];
    private static readonly string[] FailModes = [RateLimitPolicy.FailModeOpen, RateLimitPolicy.FailModeClosed];

    public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
    {
        var failures = new List<string>();

        if (options.Policies.Count == 0)
            failures.Add("RateLimiting:Policies must define at least one policy.");

        foreach (var (policyName, policy) in options.Policies)
        {
            if (policy.PermitLimit <= 0)
                failures.Add($"RateLimiting:Policies:{policyName}:PermitLimit must be greater than zero.");
            if (policy.WindowSeconds <= 0)
                failures.Add($"RateLimiting:Policies:{policyName}:WindowSeconds must be greater than zero.");
            if (!Algorithms.Contains(policy.Algorithm, StringComparer.Ordinal))
                failures.Add($"RateLimiting:Policies:{policyName}:Algorithm '{policy.Algorithm}' is not one of {string.Join(", ", Algorithms)}.");
            if (!FailModes.Contains(policy.FailMode, StringComparer.OrdinalIgnoreCase))
                failures.Add($"RateLimiting:Policies:{policyName}:FailMode '{policy.FailMode}' is not one of {string.Join(", ", FailModes)}.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>Names used by endpoints that have no configured policy.</summary>
    public static IReadOnlyList<string> FindUnconfiguredPolicies(
        EndpointDataSource endpoints,
        IRateLimitPolicyProvider policies)
        => endpoints.Endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<RateLimitPolicyAttribute>()?.PolicyName)
            .Where(policyName => policyName is not null && policies.GetPolicy(policyName) is null)
            .Select(policyName => policyName!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
