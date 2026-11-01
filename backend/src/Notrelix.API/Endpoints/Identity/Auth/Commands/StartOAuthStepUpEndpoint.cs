using Microsoft.Extensions.Options;
using Notrelix.API.Extensions;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthStepUp;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class StartOAuthStepUpEndpoint
{
    public static IEndpointRouteBuilder MapStartOAuthStepUp(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/oauth/{provider}/step-up/start", HandleAsync)
            .WithName("Identity.Auth.OAuth.StepUpStart")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status412PreconditionFailed, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Start OAuth step-up (re-authentication) flow")
            .WithDescription("Redirects the authenticated user to the OAuth provider to re-authenticate for a security purpose. Completion grants a purpose-bound step-up proof; no new session is issued.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string provider,
        string? purpose,
        string? returnUrl,
        ISender sender,
        IOptions<OAuthRedirectOptions> redirectOptions)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider))
        {
            return EndpointExtensions.InvalidInput($"Invalid OAuth provider: {provider}");
        }

        if (!StepUpPurposeInput.TryParse(purpose, out var stepUpPurpose))
        {
            return EndpointExtensions.InvalidInput($"Invalid step-up purpose: {purpose}");
        }

        var command = new StartOAuthStepUpCommand
        {
            Provider = oauthProvider,
            Purpose = stepUpPurpose,
            ReturnUrl = returnUrl
        };

        var result = await sender.Send(command);

        if (!result.Succeeded || result.Data is null)
        {
            // A browser navigation: a refusal returns the person to the page that asked, not to a JSON body.
            var returnTo = Notrelix.Application.Features.Identity.OAuth.SafeReturnPath.OrNull(returnUrl);
            return returnTo is null
                ? result.ToApiResult()
                : Results.Redirect(CompleteOAuthStepUpEndpoint.FrontendPage(
                    redirectOptions.Value, $"{returnTo}{(returnTo.Contains('?') ? '&' : '?')}stepUpResult=failed"));
        }

        return Results.Redirect(result.Data.AuthorizationUrl);
    }
}