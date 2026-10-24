using Notrelix.API.RateLimiting;

namespace Notrelix.API.Middleware;

public sealed class PreAuthenticationRateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public PreAuthenticationRateLimitMiddleware(RequestDelegate next)
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

        if (policy is null || policy.PartitionBy != PartitionKey.Ip)
        {
            await _next(context);
            return;
        }

        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!await RateLimitEnforcer.TryAcquireAsync(context, attribute.PolicyName, policy, partitionKey))
            return;

        await _next(context);
    }
}
