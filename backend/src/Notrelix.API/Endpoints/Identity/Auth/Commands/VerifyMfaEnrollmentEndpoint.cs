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
            .WithTags("Identity.Mfa")
            .WithSummary("Verify MFA enrollment code and activate the authenticator")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        VerifyMfaEnrollmentRequest request,
        ISender sender)
    {
        var command = new VerifyMfaEnrollmentCommand
        {
            MfaMethodId = request.MfaMethodId,
            Code = request.Code
        };

        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}