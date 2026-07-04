using Notrelix.API.RateLimiting;

namespace Notrelix.API.Middleware;

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitingMiddleware(RequestDelegate next)
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

        if (policy is null)
        {
            await _next(context);
            return;
        }

        var partitionKey = policy.PartitionBy switch
        {
            "Ip" => context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            "UserId" => context.User.FindFirst("sub")?.Value ?? "anonymous",
            _ => "unknown",
        };

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
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] =
                decision.RetryAfter?.TotalSeconds.ToString("F0") ?? "60";
            context.Response.Headers["X-RateLimit-Limit"] = decision.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = decision.Remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] =
                decision.ResetAt.ToUnixTimeSeconds().ToString();

            await context.Response.WriteAsJsonAsync(
                new { error = "Rate limit exceeded. Please try again later." },
                context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
