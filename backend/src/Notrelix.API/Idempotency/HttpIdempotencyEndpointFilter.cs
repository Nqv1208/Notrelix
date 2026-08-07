using FluentValidation.Results;
using Notrelix.Application.Common.Idempotency;
using AppValidationException = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.API.Idempotency;

/// <summary>
/// HTTP idempotency contract (API-03 / spec 3.3).
///
/// For endpoints marked with <see cref="WithIdempotencyKey"/>:
/// - requires exactly one Idempotency-Key header (missing, empty or repeated → typed 400);
/// - validates the raw key through the scoped execution-context writer
///   (8–128 chars, no control characters, no surrounding whitespace);
/// - binds the key with source <see cref="IdempotencyExecutionSource.Http"/>
///   before dispatch so <see cref="IdempotencyBehavior{TRequest,TResponse}"/>
///   can require it;
/// - adds Idempotency-Replayed: true when the dispatch replayed a stored result.
///
/// The filter never matches exception messages; every failure path is typed.
/// </summary>
public sealed class HttpIdempotencyEndpointFilter : IEndpointFilter
{
    public const string KeyHeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotency-Replayed";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<IdempotencyKeyRequiredMetadata>() is null)
        {
            return await next(context);
        }

        var rawKey = RequireSingleKeyHeader(httpContext);

        var writer = httpContext.RequestServices
            .GetRequiredService<IIdempotencyExecutionContextWriter>();
        try
        {
            writer.Set(rawKey, IdempotencyExecutionSource.Http);
        }
        catch (ArgumentException ex)
        {
            throw new AppValidationException(new[]
            {
                new ValidationFailure(KeyHeaderName, ex.Message),
            });
        }

        var result = await next(context);

        var executionContext = httpContext.RequestServices
            .GetRequiredService<IIdempotencyExecutionContext>();
        if (executionContext.IsReplay)
        {
            httpContext.Response.Headers[ReplayedHeaderName] = "true";
        }

        return result;
    }

    private static string RequireSingleKeyHeader(HttpContext httpContext)
    {
        var values = httpContext.Request.Headers[KeyHeaderName];

        if (values.Count == 0 || string.IsNullOrWhiteSpace(values.ToString()))
        {
            throw new AppValidationException(new[]
            {
                new ValidationFailure(
                    KeyHeaderName,
                    $"The {KeyHeaderName} header is required for this idempotent operation."),
            });
        }

        if (values.Count > 1)
        {
            throw new AppValidationException(new[]
            {
                new ValidationFailure(
                    KeyHeaderName,
                    $"Exactly one {KeyHeaderName} header is allowed."),
            });
        }

        return values.ToString();
    }
}

/// <summary>
/// Extension methods for registering HTTP idempotency on endpoints.
/// </summary>
public static class IdempotencyEndpointExtensions
{
    /// <summary>
    /// Marks an endpoint as requiring an Idempotency-Key header. The endpoint
    /// must dispatch an idempotent Application request; GET/HEAD endpoints may
    /// never be marked (enforced by the endpoint contract architecture gate).
    /// </summary>
    public static RouteHandlerBuilder WithIdempotencyKey(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter<HttpIdempotencyEndpointFilter>();
        builder.WithMetadata(new IdempotencyKeyRequiredMetadata());
        return builder;
    }
}

/// <summary>
/// Metadata marker indicating an endpoint requires the Idempotency-Key header.
/// Consumed by the endpoint filter and the OpenAPI operation filter.
/// </summary>
public sealed class IdempotencyKeyRequiredMetadata;
