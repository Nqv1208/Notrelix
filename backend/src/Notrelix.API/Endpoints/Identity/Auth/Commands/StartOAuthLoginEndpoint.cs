using Microsoft.Extensions.Options;
using Notrelix.API.Extensions;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class StartOAuthLoginEndpoint
{
    public static IEndpointRouteBuilder MapStartOAuthLogin(this IEndpointRouteBuilder group)
    {
        group.MapPublicGet("/oauth/{provider}/start", HandleAsync)
            .WithName("Identity.Auth.OAuth.Start")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblems(StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Start OAuth login flow")
            .WithDescription("Redirects the browser to the OAuth provider's authorization endpoint. An unknown, disabled or unconfigured provider redirects to the sign-in page with error=identity.oauth.provider-unavailable instead; no redirect to a provider is made.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string provider,
        string? returnUrl,
        ISender sender,
        IOptions<OAuthRedirectOptions> redirectOptions)
    {
        // The browser navigates here, so a refusal is the sign-in page saying so, never raw JSON.
        var unavailable = Results.Redirect(CompleteOAuthLoginEndpoint.AppendErrorCode(
            redirectOptions.Value.FrontendFailureUrl, StartOAuthLoginCommandHandler.ProviderUnavailableCode));

        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider)
            || !Enum.IsDefined(oauthProvider))
        {
            return unavailable;
        }

        var command = new StartOAuthLoginCommand
        {
            Provider = oauthProvider,
            ReturnUrl = returnUrl
        };

        var result = await sender.Send(command);

        if (!result.Succeeded || result.Data is null)
        {
            return unavailable;
        }

        return Results.Redirect(result.Data.AuthorizationUrl);
    }
}
