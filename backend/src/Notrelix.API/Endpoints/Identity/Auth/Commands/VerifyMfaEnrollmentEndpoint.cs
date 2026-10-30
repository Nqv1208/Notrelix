using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Mfa.Commands.VerifyMfaEnrollment;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class VerifyMfaEnrollmentEndpoint
{
    public static IEndpointRouteBuilder MapVerifyMfaEnrollment(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/mfa/enrollment/verify", HandleAsync)
            .WithName("Identity.Mfa.VerifyEnrollment")
            .Produces<MfaEnrollmentVerifyResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Mfa")
            .WithSummary("Verify MFA enrollment code and activate the authenticator")
            .WithDescription("A wrong code leaves the method pending. The recovery codes in the answer are shown once and never readable again.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        VerifyMfaEnrollmentRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        // The answer holds recovery codes that are shown once: nothing may keep a copy.
        httpContext.Response.Headers.CacheControl = "no-store";
        var command = new VerifyMfaEnrollmentCommand
        {
            MfaMethodId = request.MfaMethodId,
            Code = request.Code
        };

        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}