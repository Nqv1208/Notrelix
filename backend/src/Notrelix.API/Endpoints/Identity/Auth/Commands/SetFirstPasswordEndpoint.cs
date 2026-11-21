using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Auth.Commands.SetFirstPassword;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public sealed record SetFirstPasswordRequest(string NewPassword, string StepUpToken = "");

public static class SetFirstPasswordEndpoint
{
    public static IEndpointRouteBuilder MapSetFirstPassword(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/set-password", HandleAsync)
            .WithName("Identity.Auth.SetFirstPassword")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Set a first password for a person who signs in with a provider")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));
        return group;
    }

    private static async Task<IResult> HandleAsync(SetFirstPasswordRequest request, ISender sender)
    {
        var result = await sender.Send(new SetFirstPasswordCommand(request.NewPassword, request.StepUpToken));
        return result.ToApiResult();
    }
}
