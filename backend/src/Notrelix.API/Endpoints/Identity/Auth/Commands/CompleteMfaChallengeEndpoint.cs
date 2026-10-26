using Notrelix.API.Contracts.Identity;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteMfaChallengeEndpoint
{
    public static IEndpointRouteBuilder MapCompleteMfaChallenge(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/mfa/verify", HandleAsync)
            .WithName("Identity.Mfa.VerifyChallenge")
            .Produces<AuthSessionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Mfa")
            .WithSummary("Complete an MFA challenge with a TOTP or recovery code")
            .WithDescription("The challenge comes from the request body or, after an OAuth sign-in, from the httpOnly mfaChallenge cookie. A body token wins. The cookie is cleared on success and when the challenge is expired or exhausted.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        CompleteMfaChallengeRequest request,
        ISender sender,
        ICookieService cookieService)
    {
        var useCookie = string.IsNullOrWhiteSpace(request.ChallengeToken);
        var challengeToken = useCookie ? cookieService.GetMfaChallengeCookie() : request.ChallengeToken;

        if (string.IsNullOrWhiteSpace(challengeToken))
        {
            // No challenge in the body and no (or an expired) cookie: nothing can be verified.
            return Result<AuthResult>.Failure(CompleteMfaChallengeErrors.ChallengeInvalid).ToSessionApiResult();
        }

        var command = new CompleteMfaChallengeCommand
        {
            ChallengeToken = challengeToken,
            Code = request.Code
        };

        var result = await sender.Send(command);

        if (result.Succeeded && result.Data is not null)
        {
            cookieService.SetTokenCookie(result.Data.AccessToken!, result.Data.RefreshToken!);
            cookieService.DeleteMfaChallengeCookie();
        }
        else if (useCookie && result.TypedErrors.Any(error => error.Code == CompleteMfaChallengeErrors.ChallengeInvalidCode))
        {
            // The challenge is gone (expired or out of attempts): the cookie can never work again.
            cookieService.DeleteMfaChallengeCookie();
        }

        return result.ToSessionApiResult();
    }
}
