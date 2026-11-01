using Notrelix.API.Extensions;
using Microsoft.Extensions.Options;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthStepUp;
using Notrelix.Application.Features.Identity.OAuth.DTOs;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteOAuthStepUpEndpoint
{
    public static IEndpointRouteBuilder MapCompleteOAuthStepUp(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/oauth/{provider}/step-up/callback", HandleAsync)
            .WithName("Identity.Auth.OAuth.StepUpCallback")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Complete OAuth step-up callback")
            .WithDescription(
                "Handles the OAuth provider callback for an authenticated step-up (re-authentication) flow and grants a purpose-bound step-up proof. " +
                "The proof travels in a short-lived httpOnly cookie read once by POST /security/step-up/claim-provider, never in the URL; " +
                "the browser returns to the internal path given at start (stepUpResult=failed is appended on failure).")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string provider,
        string? code,
        string? state,
        string? error,
        string? error_description,
        ISender sender,
        IOAuthStateStore stateStore,
        ICookieService cookieService,
        IOptions<OAuthRedirectOptions> redirectOptions)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider))
        {
            return Results.Redirect(redirectOptions.Value.FrontendFailureUrl);
        }

        // The command consumes the state, so the return path is read from the peeked copy; only an
        // internal root-relative path ever leaves the API.
        var storedState = string.IsNullOrWhiteSpace(state)
            ? null
            : await stateStore.PeekAsync(state, CancellationToken.None);
        var returnTo = storedState?.Flow == OAuthFlowKind.StepUp ? SafeReturnPath.OrNull(storedState.ReturnUrl) : null;

        var command = new CompleteOAuthStepUpCommand
        {
            Provider = oauthProvider,
            Code = code ?? string.Empty,
            State = state ?? string.Empty,
            Error = error,
            ErrorDescription = error_description
        };

        var result = await sender.Send(command);

        if (!result.Succeeded || result.Data is null)
        {
            return Results.Redirect(returnTo is null
                ? redirectOptions.Value.FrontendFailureUrl
                : FrontendPage(redirectOptions.Value, $"{returnTo}{(returnTo.Contains('?') ? '&' : '?')}stepUpResult=failed"));
        }

        var proof = result.Data.Proof;
        cookieService.SetStepUpProofCookie(
            result.Data.SpareProof is null ? proof.ProofToken : $"{proof.ProofToken}.{result.Data.SpareProof.ProofToken}",
            proof.ExpiresAt);

        return Results.Redirect(returnTo is null
            ? redirectOptions.Value.FrontendSuccessUrl
            : FrontendPage(redirectOptions.Value, returnTo));
    }

    /// <summary>
    /// The browser is on the API host here: an internal path must be sent to the web app, whose origin
    /// is the one of the configured success URL (a relative success URL means one host serves both).
    /// </summary>
    internal static string FrontendPage(OAuthRedirectOptions options, string internalPath) =>
        Uri.TryCreate(options.FrontendSuccessUrl, UriKind.Absolute, out var origin)
        && origin.Scheme is "http" or "https"
            ? new Uri(origin, internalPath).ToString()
            : internalPath;
}