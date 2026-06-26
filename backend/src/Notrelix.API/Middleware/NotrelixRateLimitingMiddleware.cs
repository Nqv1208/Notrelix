using System.Security.Claims;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.RateLimiting;
using Notrelix.API.RateLimiting;

namespace Notrelix.API.Middleware;

public sealed class NotrelixRateLimitingMiddleware
{
    private readonly RequestDelegate _next;

    public NotrelixRateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRateLimitService rateLimitService,
        IRateLimitPolicyProvider policyProvider)
    {
        var endpoint = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<RateLimitPolicyAttribute>();

        if (attribute is null)
        {
            await _next(context);
            return;
        }

        var policy = policyProvider.GetPolicy(attribute.PolicyName);
        if (policy is null)
        {
            await _next(context);
            return;
        }

        var partitionKey = GetPartitionKey(context, policy.PartitionBy);
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
                attribute.PolicyName,
                partitionKey,
                policy.PermitLimit,
                TimeSpan.FromSeconds(policy.WindowSeconds),
                algorithm);
        }
        catch
        {
            if (policy.FailMode == "Closed")
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/503",
                    title = "Service Unavailable",
                    detail = "Rate limit service unavailable. Try again later.",
                    status = 503,
                });
                return;
            }

            await _next(context);
            return;
        }

        if (decision.IsAllowed)
        {
            context.Response.Headers["X-RateLimit-Limit"] = decision.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = decision.Remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] =
                decision.ResetAt.ToUnixTimeSeconds().ToString();
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter =
            decision.RetryAfter?.TotalSeconds.ToString("F0") ?? "60";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/429",
            title = "Too Many Requests",
            detail = $"Rate limit exceeded. Try again in {decision.RetryAfter?.TotalSeconds:F0}s.",
            status = 429,
        });
    }

    private static string GetPartitionKey(HttpContext context, string partitionBy)
    {
        return partitionBy switch
        {
            "Ip" => context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            "UserId" => context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
            _ => context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        };
    }
}
