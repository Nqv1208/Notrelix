using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class StartOAuthLoginEndpoint
{
    public static IEndpointRouteBuilder MapStartOAuthLogin(this IEndpointRouteBuilder group)
    {
        group.MapPublicGet("/oauth/{provider}/start", HandleAsync)
            .WithName("Identity.Auth.OAuth.Start")
            .WithTags("Identity.Auth")
            .WithSummary("Start OAuth login flow")
            .WithDescription("Redirects user to the OAuth provider's authorization endpoint.")
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
            return EndpointExtensions.InvalidInput($"Invalid OAuth provider: {provider}");
        }

        var command = new StartOAuthLoginCommand
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
