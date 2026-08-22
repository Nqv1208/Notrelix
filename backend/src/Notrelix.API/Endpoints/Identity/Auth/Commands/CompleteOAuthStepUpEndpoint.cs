using Microsoft.Extensions.Options;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthStepUp;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class CompleteOAuthStepUpEndpoint
{
    public static IEndpointRouteBuilder MapCompleteOAuthStepUp(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/oauth/{provider}/step-up/callback", HandleAsync)
            .WithName("Identity.Auth.OAuth.StepUpCallback")
            .WithTags("Identity.Auth")
            .WithSummary("Complete OAuth step-up callback")
            .WithDescription("Handles the OAuth provider callback for an authenticated step-up (re-authentication) flow and grants a purpose-bound step-up proof.")
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

        var command = new CompleteOAuthStepUpCommand
        {
            Provider = oauthProvider,
            Code = code ?? string.Empty,
            State = state ?? string.Empty,
            Error = error,
            ErrorDescription = error_description
        };

        var result = await sender.Send(command);

        if (!result.Succeeded)
        {
            return Results.Redirect(redirectOptions.Value.FrontendFailureUrl);
        }

        return Results.Redirect(redirectOptions.Value.FrontendSuccessUrl);
    }
}