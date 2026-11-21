using Microsoft.Extensions.Options;
using Notrelix.API.Extensions;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Sso.Commands.StartSsoSignIn;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public sealed record StartSsoSignInRequest(string Email, string? ReturnUrl = null);

public static class StartSsoSignInEndpoint
{
    public static IEndpointRouteBuilder MapStartSsoSignIn(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/sso/start", HandleAsync)
            .WithName("Identity.Auth.Sso.Start")
            .Produces<SsoStartResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Start a company sign-in for an email")
            .WithDescription("Routes the email's verified domain to its Account's identity provider and answers the provider's authorization address (authorization code with PKCE). Every email that cannot sign in through a company provider gets the same 404 identity.sso.unavailable. Rate limited per IP.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        StartSsoSignInRequest request,
        ISender sender,
        IOptions<SsoRedirectOptions> redirectOptions,
        HttpContext http)
    {
        var result = await sender.Send(new StartSsoSignInCommand
        {
            Email = request.Email,
            ReturnUrl = request.ReturnUrl,
            RedirectUri = redirectOptions.Value.CallbackUrl(http.Request),
        });
        return result.ToApiResult();
    }
}
