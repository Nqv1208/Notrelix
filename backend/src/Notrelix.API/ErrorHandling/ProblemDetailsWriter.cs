using System.Diagnostics;

namespace Notrelix.API.ErrorHandling;

public static class ProblemDetailsWriter
{
    public static Task WriteTooManyRequestsAsync(HttpContext context, int retryAfterSeconds, int limit, int remaining, DateTimeOffset resetAt, CancellationToken ct)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://docs.notrelix.com/problems/rate-limit-exceeded",
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please try again later.",
            Status = StatusCodes.Status429TooManyRequests,
            Instance = context.Request.Path,
        };

        problemDetails.Extensions["errorCode"] = ErrorCodes.TooManyRequests;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? "unknown";
        problemDetails.Extensions["retryAfter"] = retryAfterSeconds;
        problemDetails.Extensions["limit"] = limit;
        problemDetails.Extensions["remaining"] = remaining;
        problemDetails.Extensions["resetAt"] = resetAt.ToUnixTimeSeconds();

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetAt.ToUnixTimeSeconds().ToString();

        return context.Response.WriteAsJsonAsync(problemDetails, ct);
    }
}
