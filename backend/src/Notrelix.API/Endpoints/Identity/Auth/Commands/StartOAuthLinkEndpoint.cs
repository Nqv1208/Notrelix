using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLink;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class StartOAuthLinkEndpoint
{
    public static IEndpointRouteBuilder MapStartOAuthLink(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/oauth/{provider}/link/start", HandleAsync)
            .WithName("Identity.Auth.OAuth.LinkStart")
            .WithTags("Identity.Auth")
            .WithSummary("Start OAuth link flow")
            .WithDescription("Redirects an authenticated user to the OAuth provider's authorization endpoint to link a provider identity.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string provider,
        string? returnUrl,
        ISender sender)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider))
        {
            return Results.BadRequest(new { error = $"Invalid OAuth provider: {provider}" });
        }

        var command = new StartOAuthLinkCommand
        {
            Provider = oauthProvider,
            ReturnUrl = returnUrl
        };

        var result = await sender.Send(command);

        if (!result.Succeeded || result.Data is null)
        {
            return result.ToApiResult();
        }

        return Results.Redirect(result.Data.AuthorizationUrl);
    }
}
