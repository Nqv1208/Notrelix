using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Notrelix.API.ErrorHandling;

public static class ProblemDetailsOptionsSetup
{
    public static void Customize(ProblemDetailsOptions options)
    {
        options.CustomizeProblemDetails = context =>
        {
            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions["traceId"] = traceId;
        };
    }
}
