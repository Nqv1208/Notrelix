using Microsoft.Extensions.Options;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLink;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteOAuthLinkEndpoint
{
    public static IEndpointRouteBuilder MapCompleteOAuthLink(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/oauth/{provider}/link/callback", HandleAsync)
            .WithName("Identity.Auth.OAuth.LinkCallback")
            .WithTags("Identity.Auth")
            .WithSummary("Complete OAuth link callback")
            .WithDescription("Handles the OAuth provider callback for an authenticated link flow, validates state, and links the provider identity to the current user.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string provider,
        string? code,
        string? state,
        string? error,
        string? error_description,
        ISender sender,
        IOptions<OAuthRedirectOptions> redirectOptions)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider))
        {
            return Results.Redirect(redirectOptions.Value.FrontendFailureUrl);
        }

        var command = new CompleteOAuthLinkCommand
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
            return Results.Redirect(redirectOptions.Value.FrontendFailureUrl);
        }

        return Results.Redirect(redirectOptions.Value.FrontendSuccessUrl);
    }
}
