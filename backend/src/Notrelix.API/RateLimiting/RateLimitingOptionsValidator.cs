using Microsoft.Extensions.Options;
using Notrelix.API.Options;

namespace Notrelix.API.RateLimiting;

/// <summary>
/// Validates the configured rate-limit policies and proves that every policy name declared by an
/// endpoint through <see cref="RateLimitPolicyAttribute"/> resolves to a configured policy, so a
/// misnamed or unbound policy stops the host instead of silently disabling a limit.
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

    public static void EnsureEveryDeclaredPolicyIsConfigured(
        EndpointDataSource endpoints,
        IRateLimitPolicyProvider policies)
    {
        var missing = FindUnconfiguredPolicies(endpoints, policies);
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Endpoints declare rate-limit policies that are not configured under RateLimiting:Policies: "
                + string.Join(", ", missing) + ".");
        }
    }
}
