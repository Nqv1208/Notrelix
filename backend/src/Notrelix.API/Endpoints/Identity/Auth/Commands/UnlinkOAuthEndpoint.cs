using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Commands.UnlinkOAuth;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class UnlinkOAuthEndpoint
{
    public static IEndpointRouteBuilder MapUnlinkOAuth(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/oauth/{provider}/unlink", HandleAsync)
            .WithName("Identity.Auth.OAuth.Unlink")
            .WithTags("Identity.Auth")
            .WithSummary("Unlink an OAuth provider identity")
            .WithDescription("Removes a linked provider identity from the current user, protected by the last-primary-auth-method invariant.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string provider,
        ISender sender)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider))
        {
            return Results.BadRequest(new { error = $"Invalid OAuth provider: {provider}" });
        }

        var command = new UnlinkOAuthCommand
        {
            Provider = oauthProvider
        };

        var result = await sender.Send(command);

        return result.ToApiResult();
    }
}
