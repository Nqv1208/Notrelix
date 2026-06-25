using System.Diagnostics;

namespace Notrelix.API.ErrorHandling;

public static class ProblemDetailsExtensions
{
    public static ProblemHttpResult ToProblemHttpResult(this ProblemDetails problemDetails)
    {
        return TypedResults.Problem(problemDetails);
    }

    public static ProblemHttpResult ToValidationProblem(this IDictionary<string, string[]> errors)
    {
        var pd = new HttpValidationProblemDetails(errors)
        {
            Type = "https://docs.notrelix.com/problems/validation-failed",
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
        };

        pd.Extensions["errorCode"] = ErrorCodes.ValidationFailed;
        pd.Extensions["traceId"] = Activity.Current?.Id ?? "unknown";

        return TypedResults.Problem(pd);
    }
}
