using Microsoft.Extensions.Options;
using Notrelix.API.Extensions;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.Sso;
using Notrelix.Application.Features.Identity.Sso.Commands.CompleteSsoSignIn;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteSsoSignInEndpoint
{
    public static IEndpointRouteBuilder MapCompleteSsoSignIn(this IEndpointRouteBuilder group)
    {
        group.MapPublicGet("/sso/callback", HandleAsync)
            .WithName("Identity.Auth.Sso.Callback")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblems(StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Complete a company sign-in")
            .WithDescription("The one callback address of every company identity provider. Uses up the state, exchanges the code, checks the provider's ID token and finds, links or provisions the person, then sets the auth cookies and redirects. Every failure redirects to the sign-in page with one code: identity.sso.state-invalid, denied, failed, email-unverified, domain-mismatch, identity-conflict, not-provisioned or unavailable (or identity.oauth.mfa-required when the second factor cannot start).")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string? code,
        string? state,
        string? error,
        string? error_description,
        ISender sender,
        ICookieService cookieService,
        IOptions<OAuthRedirectOptions> redirectOptions)
    {
        var options = redirectOptions.Value;
        var result = await sender.Send(new CompleteSsoSignInCommand
        {
            Code = code,
            State = state,
            Error = error,
            ErrorDescription = error_description,
        });

        // Every failure lands on the sign-in page with one normalized code, never the raw message.
        if (!result.Succeeded || result.Data is null)
        {
            return Results.Redirect(CompleteOAuthLoginEndpoint.AppendErrorCode(
                options.FrontendFailureUrl, SsoErrorCodes.Normalize(result.TypedErrors.FirstOrDefault()?.Code)));
        }

        var auth = result.Data.Auth;
        var returnTo = result.Data.ReturnUrl;
        var mfaFailure = Results.Redirect(CompleteOAuthLoginEndpoint.AppendErrorCode(
            options.FrontendFailureUrl, OAuthLoginErrorCodes.MfaRequired));

        // A person with a second factor has no session yet: the challenge travels in a short-lived httpOnly
        // cookie, never in the address.
        if (auth.MfaRequired)
        {
            if (string.IsNullOrEmpty(auth.MfaChallengeToken) || auth.MfaExpiresAt is null)
                return mfaFailure;

            cookieService.SetMfaChallengeCookie(
                auth.MfaChallengeToken, new DateTimeOffset(DateTime.SpecifyKind(auth.MfaExpiresAt.Value, DateTimeKind.Utc)));
            return Results.Redirect(CompleteOAuthLoginEndpoint.AppendReturnTo(options.FrontendMfaUrl, returnTo));
        }

        // The Account requires MFA and the person has no factor: same cookie and page, told to enrol one.
        if (auth.MfaEnrollmentRequired)
        {
            if (string.IsNullOrEmpty(auth.MfaEnrollmentToken) || auth.MfaExpiresAt is null)
                return mfaFailure;

            cookieService.SetMfaChallengeCookie(
                auth.MfaEnrollmentToken, new DateTimeOffset(DateTime.SpecifyKind(auth.MfaExpiresAt.Value, DateTimeKind.Utc)));
            var mfaUrl = CompleteOAuthLoginEndpoint.AppendReturnTo(options.FrontendMfaUrl, returnTo);
            return Results.Redirect($"{mfaUrl}{(mfaUrl.Contains('?') ? '&' : '?')}enroll=1");
        }

        if (string.IsNullOrEmpty(auth.AccessToken) || string.IsNullOrEmpty(auth.RefreshToken))
            return mfaFailure;

        cookieService.SetTokenCookie(auth.AccessToken, auth.RefreshToken);
        return Results.Redirect(CompleteOAuthLoginEndpoint.AppendReturnTo(options.FrontendSuccessUrl, returnTo));
    }
}
