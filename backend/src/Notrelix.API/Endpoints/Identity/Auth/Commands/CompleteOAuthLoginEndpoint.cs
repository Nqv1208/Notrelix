using Microsoft.Extensions.Options;
using Notrelix.API.Extensions;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteOAuthLoginEndpoint
{
    public static IEndpointRouteBuilder MapCompleteOAuthLogin(this IEndpointRouteBuilder group)
    {
        group.MapPublicGet("/oauth/{provider}/callback", HandleAsync)
            .WithName("Identity.Auth.OAuth.Callback")
            .WithTags("Identity.Auth")
            .WithSummary("Complete OAuth login callback")
            .WithDescription("Handles the OAuth provider callback, validates state, exchanges code, and sets auth cookies.")
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
        ICookieService cookieService,
        IOptions<OAuthRedirectOptions> redirectOptions)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider))
        {
            return Results.Redirect(redirectOptions.Value.FrontendFailureUrl);
        }

        var command = new CompleteOAuthLoginCommand
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

        cookieService.SetTokenCookie(result.Data.AccessToken, result.Data.RefreshToken);

        var returnUrl = redirectOptions.Value.FrontendSuccessUrl;
        return Results.Redirect(returnUrl);
    }
}
