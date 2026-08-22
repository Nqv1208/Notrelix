using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteMfaChallengeEndpoint
{
    public static IEndpointRouteBuilder MapCompleteMfaChallenge(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/mfa/verify", HandleAsync)
            .WithName("Identity.Mfa.VerifyChallenge")
            .WithTags("Identity.Mfa")
            .WithSummary("Complete an MFA challenge with a TOTP or recovery code")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        CompleteMfaChallengeRequest request,
        ISender sender,
        ICookieService cookieService)
    {
        var command = new CompleteMfaChallengeCommand
        {
            ChallengeToken = request.ChallengeToken,
            Code = request.Code
        };

        var result = await sender.Send(command);

        if (result.Succeeded && result.Data is not null)
        {
            cookieService.SetTokenCookie(result.Data.AccessToken!, result.Data.RefreshToken!);
        }

        return result.ToApiResult();
    }
}