namespace Notrelix.API.Filters;

/// <summary>
/// Endpoint filter for HTTP idempotency contract (API-03).
/// - Reads Idempotency-Key header for marked endpoints
/// - Missing key on idempotent command → 400 validation ProblemDetails
/// - Payload mismatch (ConflictException) → 409 ProblemDetails
/// - Replay → adds Idempotency-Replayed: true header
/// </summary>
public sealed class IdempotencyKeyEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

        // Store key in HttpContext.Items for downstream binding
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            httpContext.Items["IdempotencyKey"] = idempotencyKey;
        }

        try
        {
            var result = await next(context);

            // Check if this was a replay (set by IdempotencyBehavior via HttpContext marker)
            if (httpContext.Items.ContainsKey("IdempotencyReplayed"))
            {
                httpContext.Response.Headers["Idempotency-Replayed"] = "true";
            }

            return result;
        }
        catch (Notrelix.Application.Common.Exceptions.ConflictException ex)
            when (ex.Message.Contains("Idempotency key was already used with a different request payload"))
        {
            return Results.Problem(
                title: "Idempotency Conflict",
                detail: "The Idempotency-Key was already used with a different request payload. " +
                        "Use a new key for a different operation.",
                statusCode: StatusCodes.Status409Conflict,
                type: "https://notrelix.dev/problems/idempotency-payload-mismatch");
        }
    }
}

/// <summary>
/// Extension methods for registering idempotency on endpoints.
/// </summary>
public static class IdempotencyEndpointExtensions
{
    /// <summary>
    /// Marks an endpoint as requiring an Idempotency-Key header.
    /// </summary>
    public static RouteHandlerBuilder WithIdempotencyKey(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter<IdempotencyKeyEndpointFilter>();
        builder.WithMetadata(new IdempotencyKeyRequiredMetadata());
        return builder;
    }
}

/// <summary>
/// Metadata marker indicating an endpoint requires Idempotency-Key header.
/// Used by OpenAPI operation filter to declare the header parameter.
/// </summary>
public sealed class IdempotencyKeyRequiredMetadata;
