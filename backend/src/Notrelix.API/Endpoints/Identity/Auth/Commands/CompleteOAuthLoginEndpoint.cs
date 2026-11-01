using Notrelix.API.Extensions;
using Microsoft.Extensions.Options;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteOAuthLoginEndpoint
{
    public static IEndpointRouteBuilder MapCompleteOAuthLogin(this IEndpointRouteBuilder group)
    {
        group.MapPublicGet("/oauth/{provider}/callback", HandleAsync)
            .WithName("Identity.Auth.OAuth.Callback")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblems(StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Complete OAuth login callback")
            .WithDescription("Handles the OAuth provider callback, validates state, exchanges code, checks the provider ID token, and sets auth cookies. Every failure redirects to the sign-in page with one normalized error code: identity.oauth.failed, state-invalid, denied, email-unverified, email-in-use, mfa-required or provider-unavailable.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string provider,
        string? code,
        string? state,
        string? error,
        string? error_description,
        ISender sender,
        ICookieService cookieService,
        IOAuthStateStore stateStore,
        IOptions<OAuthRedirectOptions> redirectOptions)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider)
            || !Enum.IsDefined(oauthProvider))
        {
            return Results.Redirect(AppendErrorCode(
                redirectOptions.Value.FrontendFailureUrl, OAuthLoginErrorCodes.ProviderUnavailable));
        }

        OAuthLoginState? storedState = null;
        if (!string.IsNullOrWhiteSpace(state))
        {
            storedState = await stateStore.PeekAsync(state, CancellationToken.None);
            if (storedState is not null && storedState.Flow != OAuthFlowKind.Login)
            {
                return Results.Redirect(BuildFlowHandoffUrl(
                    redirectOptions.Value.FrontendSuccessUrl, code, state, error, error_description));
            }
        }

        // The login command consumes the state, so the return path is read from the peeked copy.
        // Only an internal root-relative path ever leaves the API; anything else is dropped.
        var returnTo = SafeReturnPath.OrNull(storedState?.ReturnUrl);

        var command = new CompleteOAuthLoginCommand
        {
            Provider = oauthProvider,
            Code = code ?? string.Empty,
            State = state ?? string.Empty,
            Error = error,
            ErrorDescription = error_description
        };

        var result = await sender.Send(command);

        // Every failure lands on the sign-in page with one normalized code (never the raw message).
        if (!result.Succeeded || result.Data is null)
        {
            return Results.Redirect(AppendErrorCode(
                redirectOptions.Value.FrontendFailureUrl,
                OAuthLoginErrorCodes.Normalize(result.TypedErrors.FirstOrDefault()?.Code)));
        }

        // An MFA-enabled user has no session yet: the result carries a challenge, not tokens. The
        // challenge travels in a short-lived httpOnly cookie scoped to the MFA endpoints, never in
        // the URL; the browser lands on the sign-in second step and the verify endpoint reads it.
        if (result.Data.MfaRequired)
        {
            if (string.IsNullOrEmpty(result.Data.MfaChallengeToken) || result.Data.MfaExpiresAt is null)
            {
                return Results.Redirect(AppendErrorCode(redirectOptions.Value.FrontendFailureUrl, MfaRequiredErrorCode));
            }

            cookieService.SetMfaChallengeCookie(
                result.Data.MfaChallengeToken,
                new DateTimeOffset(DateTime.SpecifyKind(result.Data.MfaExpiresAt.Value, DateTimeKind.Utc)));

            return Results.Redirect(AppendReturnTo(redirectOptions.Value.FrontendMfaUrl, returnTo));
        }

        if (string.IsNullOrEmpty(result.Data.AccessToken) || string.IsNullOrEmpty(result.Data.RefreshToken))
        {
            return Results.Redirect(AppendErrorCode(redirectOptions.Value.FrontendFailureUrl, MfaRequiredErrorCode));
        }

        cookieService.SetTokenCookie(result.Data.AccessToken, result.Data.RefreshToken);

        return Results.Redirect(AppendReturnTo(redirectOptions.Value.FrontendSuccessUrl, returnTo));
    }

    internal const string MfaRequiredErrorCode = OAuthLoginErrorCodes.MfaRequired;

    internal static string AppendReturnTo(string baseUrl, string? safeReturnTo) =>
        safeReturnTo is null
            ? baseUrl
            : $"{baseUrl}{(baseUrl.Contains('?') ? '&' : '?')}returnTo={Uri.EscapeDataString(safeReturnTo)}";

    internal static string AppendErrorCode(string baseUrl, string errorCode) =>
        $"{baseUrl}{(baseUrl.Contains('?') ? '&' : '?')}error={Uri.EscapeDataString(errorCode)}";

    private static string BuildFlowHandoffUrl(
        string baseUrl, string? code, string state, string? error, string? errorDescription)
    {
        var query = new List<string>
        {
            $"state={Uri.EscapeDataString(state)}"
        };

        if (!string.IsNullOrWhiteSpace(code))
            query.Add($"code={Uri.EscapeDataString(code)}");
        if (!string.IsNullOrWhiteSpace(error))
            query.Add($"error={Uri.EscapeDataString(error)}");
        if (!string.IsNullOrWhiteSpace(errorDescription))
            query.Add($"error_description={Uri.EscapeDataString(errorDescription)}");

        return $"{baseUrl}{(baseUrl.Contains('?') ? '&' : '?')}{string.Join('&', query)}";
    }
}
