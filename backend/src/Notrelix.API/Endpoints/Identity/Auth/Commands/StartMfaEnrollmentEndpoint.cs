using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Mfa.Commands.StartMfaEnrollment;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class StartMfaEnrollmentEndpoint
{
    public static IEndpointRouteBuilder MapStartMfaEnrollment(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/mfa/enrollment/start", HandleAsync)
            .WithName("Identity.Mfa.StartEnrollment")
            .Produces<MfaEnrollmentStartResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status409Conflict)
            .WithTags("Identity.Mfa")
            .WithSummary("Start MFA authenticator app enrollment");
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new StartMfaEnrollmentCommand());
        return result.ToApiResult();
    }
}