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

        return ResultToProblemDetails(result.Errors);
    }

    public static IResult ToApiResult<T>(this Result<T> result)
    {
        if (result.Succeeded)
            return Results.Ok(result.Data);

        return ResultToProblemDetails(result.Errors);
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, string? location = null)
    {
        if (!result.Succeeded)
            return ResultToProblemDetails(result.Errors);

        return location is not null
            ? Results.Created(location, result.Data)
            : Results.Created($"/{result.Data}", result.Data);
    }

    public static IResult ToNoContentResult(this Result result)
    {
        if (result.Succeeded)
            return Results.NoContent();

        return ResultToProblemDetails(result.Errors);
    }

    private static ProblemHttpResult ResultToProblemDetails(string[] errors)
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
