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
                var userId = context.User.FindFirst("sub")?.Value
                    ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    // No authenticated principal: authorization rejects the request with 401.
                    await _next(context);
                    return;
                }

                partitionKey = userId;
                break;
            case PartitionKey.ApiKey:
                partitionKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
                    ?? throw new UnauthorizedAccessException("API key required for rate limiting");
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported PartitionKey '{policy.PartitionBy}' in authenticated rate limiting. " +
                    "Only PartitionKey.UserId and PartitionKey.ApiKey are supported at the API middleware level.");
        }

        if (!await RateLimitEnforcer.TryAcquireAsync(context, attribute.PolicyName, policy, partitionKey))
            return;

        await _next(context);
    }
}
