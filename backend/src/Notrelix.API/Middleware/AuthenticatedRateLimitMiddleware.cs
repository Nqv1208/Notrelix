using Notrelix.API.ErrorHandling;
using Notrelix.API.RateLimiting;

namespace Notrelix.API.Middleware;

public sealed class AuthenticatedRateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticatedRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<RateLimitPolicyAttribute>();

        if (attribute is null)
        {
            await _next(context);
            return;
        }

        var provider = context.RequestServices.GetRequiredService<IRateLimitPolicyProvider>();
        var policy = provider.GetPolicy(attribute.PolicyName);

        if (policy is null || policy.PartitionBy == PartitionKey.Ip)
        {
            await _next(context);
            return;
        }

        if (policy.PartitionBy is PartitionKey.AccountId or PartitionKey.WorkspaceId)
        {
            throw new InvalidOperationException(
                $"PartitionKey.{policy.PartitionBy} rate limiting is not supported at the API middleware level. " +
                "Account/Workspace rate limiting must be configured via TenantAwareRateLimitBehavior in the Application pipeline.");
        }

        string partitionKey;
        switch (policy.PartitionBy)
        {
            case PartitionKey.UserId:
                partitionKey = context.User.FindFirst("sub")?.Value
                    ?? throw new UnauthorizedAccessException("User ID required for rate limiting");
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported PartitionKey '{policy.PartitionBy}' in authenticated rate limiting. " +
                    "Only PartitionKey.UserId is supported at the API middleware level.");
        }

        var rateLimitService = context.RequestServices.GetRequiredService<IRateLimitService>();

        var algorithm = policy.Algorithm switch
        {
            "FixedWindow" => RateLimitAlgorithm.FixedWindow,
            "TokenBucket" => RateLimitAlgorithm.TokenBucket,
            _ => RateLimitAlgorithm.SlidingWindow,
        };

        var decision = await rateLimitService.CheckAsync(
            attribute.PolicyName,
            partitionKey,
            policy.PermitLimit,
            TimeSpan.FromSeconds(policy.WindowSeconds),
            algorithm,
            context.RequestAborted);

        if (!decision.IsAllowed)
        {
            var retryAfter = (int)(decision.RetryAfter?.TotalSeconds ?? 60);
            await ProblemDetailsWriter.WriteTooManyRequestsAsync(
                context, retryAfter, decision.Limit, decision.Remaining, decision.ResetAt, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
