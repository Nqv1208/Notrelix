using Notrelix.API.ErrorHandling;

namespace Notrelix.API.RateLimiting;

/// <summary>
/// Shared decision step of the pre-authentication and authenticated rate-limit middleware:
/// consumes one permit for the partition, applies the policy fail mode when the limiter store
/// cannot decide, and writes the 429 problem response when the permit is denied.
/// </summary>
internal static class RateLimitEnforcer
{
    /// <returns><c>true</c> when the request may continue; <c>false</c> when a 429 was written.</returns>
    public static async Task<bool> TryAcquireAsync(
        HttpContext context,
        string policyName,
        RateLimitPolicy policy,
        string partitionKey)
    {
        var rateLimitService = context.RequestServices.GetRequiredService<IRateLimitService>();

        var algorithm = policy.Algorithm switch
        {
            "FixedWindow" => RateLimitAlgorithm.FixedWindow,
            "TokenBucket" => RateLimitAlgorithm.TokenBucket,
            _ => RateLimitAlgorithm.SlidingWindow,
        };

        RateLimitDecision decision;
        try
        {
            decision = await rateLimitService.CheckAsync(
                policyName,
                partitionKey,
                policy.PermitLimit,
                TimeSpan.FromSeconds(policy.WindowSeconds),
                algorithm,
                context.RequestAborted);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException
            && string.Equals(policy.FailMode, RateLimitPolicy.FailModeOpen, StringComparison.OrdinalIgnoreCase))
        {
            context.RequestServices.GetService<ILoggerFactory>()
                ?.CreateLogger("Notrelix.API.RateLimiting")
                .LogWarning(exception, "Rate limit store unavailable for policy {PolicyName}; failing open", policyName);
            return true;
        }

        if (decision.IsAllowed)
            return true;

        var retryAfter = Math.Max(1, (int)Math.Ceiling(decision.RetryAfter?.TotalSeconds ?? policy.WindowSeconds));
        await ProblemDetailsWriter.WriteTooManyRequestsAsync(
            context, retryAfter, decision.Limit, decision.Remaining, decision.ResetAt, context.RequestAborted);
        return false;
    }
}
