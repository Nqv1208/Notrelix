using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Mfa.Commands.StartMfaEnrollment;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class StartMfaEnrollmentEndpoint
{
    public static IEndpointRouteBuilder MapStartMfaEnrollment(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/mfa/enrollment/start", HandleAsync)
            .WithName("Identity.Mfa.StartEnrollment")
            .Produces<MfaEnrollmentStartResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Mfa")
            .WithSummary("Start MFA authenticator app enrollment")
            .WithDescription("Creates a pending authenticator method (replacing an earlier pending one) and returns its secret and setup URI once. The method is not active MFA until verified; an account that already has an active factor is refused with 409.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));
        return group;
    }

    private static async Task<IResult> HandleAsync(HttpContext httpContext, ISender sender)
    {
        // The answer holds a secret that is shown once: nothing may keep a copy.
        httpContext.Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new StartMfaEnrollmentCommand());
        return result.ToApiResult();
    }
}