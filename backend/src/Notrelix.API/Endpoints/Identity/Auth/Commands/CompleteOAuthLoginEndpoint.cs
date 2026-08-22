using Microsoft.Extensions.Options;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Application.Features.Identity.OAuth.DTOs;

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
        IOAuthStateStore stateStore,
        IOptions<OAuthRedirectOptions> redirectOptions)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider))
        {
            return Results.Redirect(redirectOptions.Value.FrontendFailureUrl);
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            var storedState = await stateStore.PeekAsync(state, CancellationToken.None);
            if (storedState is not null && storedState.Flow != OAuthFlowKind.Login)
            {
                return Results.Redirect(BuildFlowHandoffUrl(
                    redirectOptions.Value.FrontendSuccessUrl, code, state, error, error_description));
            }
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

    private static string BuildFlowHandoffUrl(
        string baseUrl, string? code, string state, string? error, string? errorDescription)
    {
        var query = new List<string>
        {
            $"state={Uri.EscapeDataString(state)}"
        };

        if (!string.IsNullOrWhiteSpace(code))
            query.Add($"code={Uri.EscapeDataString(code)}");
        if (!string.IsNullOrWhiteSpace(error))
            query.Add($"error={Uri.EscapeDataString(error)}");
        if (!string.IsNullOrWhiteSpace(errorDescription))
            query.Add($"error_description={Uri.EscapeDataString(errorDescription)}");

        return $"{baseUrl}{(baseUrl.Contains('?') ? '&' : '?')}{string.Join('&', query)}";
    }
}
