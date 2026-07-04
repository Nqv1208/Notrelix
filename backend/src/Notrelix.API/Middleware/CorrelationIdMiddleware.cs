namespace Notrelix.API.Middleware;

/// <summary>
/// Generates or propagates a correlation ID for request tracing.
/// Must run early in the pipeline, before any logging or processing.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid();

        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var parsedId)
            && parsedId != Guid.Empty)
        {
            correlationId = parsedId;
        }

        // Set in ICorrelationContext (for events/outbox/logs)
        var correlationContext = context.RequestServices.GetService<ICorrelationContext>();
        correlationContext?.Set(correlationId);

        // Set in IExecutionContextAccessor (for application layer)
        var executionContext = context.RequestServices.GetService<IExecutionContextAccessor>();
        executionContext?.SetCorrelation(correlationId);

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId.ToString();
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
