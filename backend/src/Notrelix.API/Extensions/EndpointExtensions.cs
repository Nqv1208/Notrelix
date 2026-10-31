using System.Diagnostics;
using Notrelix.API.ErrorHandling;
using Notrelix.Application.Common.Models;

namespace Notrelix.API.Extensions;

public static class EndpointExtensions
{
    public static IResult ToApiResult(this Result result)
    {
        if (result.Succeeded)
            return Results.Ok();

        return ResultToProblemDetails(result);
    }

    public static IResult ToApiResult<T>(this Result<T> result)
    {
        if (result.Succeeded)
            return Results.Ok(result.Data);

        return ResultToProblemDetails(result);
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, string? location = null)
    {
        if (!result.Succeeded)
            return ResultToProblemDetails(result);

        if (location is not null)
            return Results.Created(location, result.Data);

        // A DTO's ToString is not a URL (it carries dates and names that are not valid in a header).
        return result.Data is Guid or string
            ? Results.Created($"/{result.Data}", result.Data)
            : Results.Json(result.Data, statusCode: StatusCodes.Status201Created);
    }

    public static IResult ToNoContentResult(this Result result)
    {
        if (result.Succeeded)
            return Results.NoContent();

        return ResultToProblemDetails(result);
    }

    public static IResult InvalidInput(string detail)
    {
        var pd = new HttpValidationProblemDetails
        {
            Type = "https://docs.notrelix.com/problems/validation-failed",
            Title = "Validation failed",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest,
        };

        pd.Extensions["errorCode"] = ErrorCodes.ValidationFailed;
        pd.Extensions["traceId"] = Activity.Current?.Id ?? "unknown";
        pd.Errors["_errors"] = [detail];

        return TypedResults.Problem(pd);
    }

    public static IResult UnauthorizedProblem(string detail)
    {
        var pd = new ProblemDetails
        {
            Type = $"https://docs.notrelix.com/problems/{ErrorCodes.Unauthorized.Replace('.', '-')}",
            Title = "Unauthorized",
            Detail = detail,
            Status = StatusCodes.Status401Unauthorized,
        };

        pd.Extensions["errorCode"] = ErrorCodes.Unauthorized;
        pd.Extensions["traceId"] = Activity.Current?.Id ?? "unknown";

        return TypedResults.Problem(pd);
    }

    private static IResult ResultToProblemDetails(Result result)
    {
        if (result.TypedErrors.Count > 0)
        {
            return TypedFailure(result.TypedErrors);
        }

        return ValidationFailure(result.Errors);
    }

    /// <summary>
    /// Identity owns its dotted error codes (for example
    /// <c>identity.auth.invalid-credentials</c>): they are part of the public
    /// contract (D10) and are surfaced as the problem <c>errorCode</c> instead of
    /// the generic category code. Other contexts keep the generic category codes.
    /// </summary>
    private const string IdentityErrorCodePrefix = "identity.";

    private static IResult TypedFailure(IReadOnlyList<ApplicationError> errors)
    {
        var primary = errors[0];

        var (statusCode, errorCode, title, detail) = primary.Type switch
        {
            ApplicationErrorType.Validation => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "One or more validation errors occurred."),
            ApplicationErrorType.BusinessRule => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.BusinessRuleViolation,
                "Business rule violation",
                primary.Message),
            ApplicationErrorType.Authentication => (
                StatusCodes.Status401Unauthorized,
                ErrorCodes.Unauthorized,
                "Unauthorized",
                primary.Message),
            ApplicationErrorType.NotFound => (
                StatusCodes.Status404NotFound,
                ErrorCodes.ResourceNotFound,
                "Resource not found",
                primary.Message),
            ApplicationErrorType.Conflict => (
                StatusCodes.Status409Conflict,
                ErrorCodes.Conflict,
                "Conflict",
                primary.Message),
            ApplicationErrorType.TooManyRequests => (
                StatusCodes.Status429TooManyRequests,
                ErrorCodes.TooManyRequests,
                "Too many requests",
                primary.Message),
            ApplicationErrorType.PreconditionFailed => (
                StatusCodes.Status412PreconditionFailed,
                primary.Code,
                "Precondition failed",
                primary.Message),
            _ => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationFailed,
                "Validation failed",
                "One or more validation errors occurred.")
        };

        if (primary.Code.StartsWith(IdentityErrorCodePrefix, StringComparison.Ordinal))
        {
            errorCode = primary.Code;
        }

        var pd = new ProblemDetails
        {
            Type = $"https://docs.notrelix.com/problems/{errorCode.Replace('.', '-')}",
            Title = title,
            Status = statusCode,
            Detail = detail,
        };

        pd.Extensions["errorCode"] = errorCode;
        pd.Extensions["traceId"] = Activity.Current?.Id ?? "unknown";

        if (primary.RetryAfter is { } retryAfter)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            pd.Extensions["retryAfterSeconds"] = seconds;
            return new RetryAfterProblemResult(TypedResults.Problem(pd), seconds);
        }

        if (primary.Type == ApplicationErrorType.Validation && errors.Count > 0)
        {
            pd.Extensions["errors"] =
                new Dictionary<string, string[]> { ["_errors"] = errors.Select(e => e.Message).ToArray() };
        }

        return TypedResults.Problem(pd);
    }

    private static ProblemHttpResult ValidationFailure(string[] errors)
    {
        var pd = new HttpValidationProblemDetails
        {
            Type = "https://docs.notrelix.com/problems/validation-failed",
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
        };

        pd.Extensions["errorCode"] = ErrorCodes.ValidationFailed;
        pd.Extensions["traceId"] = Activity.Current?.Id ?? "unknown";

        if (errors.Length > 0)
        {
            pd.Errors["_errors"] = errors;
        }

        return TypedResults.Problem(pd);
    }
}

/// <summary>Problem response that also carries the standard Retry-After header (seconds).</summary>
internal sealed class RetryAfterProblemResult(IResult inner, int retryAfterSeconds) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return inner.ExecuteAsync(httpContext);
    }
}
