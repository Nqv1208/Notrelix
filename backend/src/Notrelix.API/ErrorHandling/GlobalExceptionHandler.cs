using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.API.ErrorHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IdempotencyOptions _idempotencyOptions;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IOptions<IdempotencyOptions> idempotencyOptions)
    {
        _logger = logger;
        _idempotencyOptions = idempotencyOptions.Value;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = ProblemDetailsMapper.Map(context, exception);

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        if (exception is IdempotencyIncompleteStateException)
        {
            // Spec 3.8: incomplete idempotency state maps to 503 + Retry-After.
            context.Response.Headers.RetryAfter =
                ((int)_idempotencyOptions.IncompleteStateRetryAfter.TotalSeconds).ToString();
        }

        if (problemDetails.Status >= 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
