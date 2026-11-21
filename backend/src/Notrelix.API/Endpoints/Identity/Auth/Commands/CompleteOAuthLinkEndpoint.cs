using Notrelix.API.Extensions;
using Microsoft.Extensions.Options;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLink;
using Notrelix.Application.Features.Identity.OAuth.DTOs;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteOAuthLinkEndpoint
{
    public static IEndpointRouteBuilder MapCompleteOAuthLink(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/oauth/{provider}/link/callback", HandleAsync)
            .WithName("Identity.Auth.OAuth.LinkCallback")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Complete OAuth link callback")
            .WithDescription("Handles the OAuth provider callback for an authenticated link flow, validates state, and links the provider identity to the current user. The browser returns to the internal path given at start with link=linked|already|collision|failed and the provider appended.")
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
        var returnTo = storedState?.Flow == OAuthFlowKind.Link ? SafeReturnPath.OrNull(storedState.ReturnUrl) : null;

        var command = new CompleteOAuthLinkCommand
        {
            Provider = oauthProvider,
            Code = code ?? string.Empty,
            State = state ?? string.Empty,
            Error = error,
            ErrorDescription = error_description
        };

        var result = await sender.Send(command);

        string Back(string outcome) => CompleteOAuthStepUpEndpoint.FrontendPage(
            redirectOptions.Value,
            $"{returnTo}{(returnTo!.Contains('?') ? '&' : '?')}link={outcome}&provider={oauthProvider}");

        if (!result.Succeeded || result.Data is null)
        {
            if (returnTo is null) return Results.Redirect(redirectOptions.Value.FrontendFailureUrl);
            var collision = result.TypedErrors.Any(e => e.Code == CompleteOAuthLinkCommandHandler.LinkCollision.Code);
            return Results.Redirect(Back(collision ? "collision" : "failed"));
        }

        return Results.Redirect(returnTo is null
            ? redirectOptions.Value.FrontendSuccessUrl
            : Back(result.Data.AlreadyLinked ? "already" : "linked"));
    }
}
