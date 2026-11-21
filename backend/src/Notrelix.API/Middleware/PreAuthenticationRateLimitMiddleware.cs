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
        var attributes = context.GetEndpoint()?.Metadata.GetOrderedMetadata<RateLimitPolicyAttribute>();

        if (attributes is null || attributes.Count == 0)
        {
            await _next(context);
            return;
        }

        var provider = context.RequestServices.GetRequiredService<IRateLimitPolicyProvider>();
        foreach (var attribute in attributes)
        {
            var policy = provider.GetPolicy(attribute.PolicyName);

            if (policy is null)
            {
                // RateLimitPolicyCatalogTests proves the shipped configuration covers every declared
                // name; a gap here comes from runtime configuration and must not pass unnoticed.
                context.RequestServices.GetRequiredService<ILogger<PreAuthenticationRateLimitMiddleware>>()
                    .LogError("Endpoint declares rate-limit policy {PolicyName}, which is not configured; the request is not limited", attribute.PolicyName);
            }

            if (policy is null || policy.PartitionBy is not (PartitionKey.Ip or PartitionKey.IpAndForm or PartitionKey.Form))
                continue;

            var partitionKey = policy.PartitionBy == PartitionKey.Form
                ? FormPartition(context)
                : (context.Connection.RemoteIpAddress?.ToString() ?? "unknown")
                    + (policy.PartitionBy == PartitionKey.IpAndForm ? "|" + FormPartition(context) : string.Empty);

            if (!await RateLimitEnforcer.TryAcquireAsync(context, attribute.PolicyName, policy, partitionKey))
                return;
        }

        await _next(context);
    }

    // A form id is canonicalised ("D" form: case, braces and hyphen-less spellings of one id share one bucket).
    // A public form key is an opaque case-sensitive token and is used as sent (it is bounded by the route).
    private static string FormPartition(HttpContext context)
    {
        if (Guid.TryParse(context.GetRouteValue("formId")?.ToString(), out var fid)) return fid.ToString("D");
        var key = context.GetRouteValue("publicKey")?.ToString();
        return string.IsNullOrEmpty(key) ? "none" : "k:" + (key.Length > 64 ? key[..64] : key);
    }
}
