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
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Mfa")
            .WithSummary("Start MFA authenticator app enrollment")
            .WithDescription("Requires a single-use step-up proof for the EnrollMfa purpose. Creates a pending authenticator method (replacing an earlier pending one) and returns its secret and setup URI once. The method is not active MFA until verified; an account that already has an active factor is refused with 409 and the proof is kept.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        StartMfaEnrollmentRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        // The answer holds a secret that is shown once: nothing may keep a copy.
        httpContext.Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new StartMfaEnrollmentCommand { StepUpToken = request.StepUpToken });
        return result.ToApiResult();
    }
}

public sealed record StartMfaEnrollmentRequest
{
    public string StepUpToken { get; init; } = string.Empty;
}
