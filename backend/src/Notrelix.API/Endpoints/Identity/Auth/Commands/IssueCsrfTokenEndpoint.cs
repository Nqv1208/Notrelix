using Notrelix.API.RateLimiting;
using Notrelix.Infrastructure.Auth.Csrf;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class IssueCsrfTokenEndpoint
{
    public static IEndpointRouteBuilder MapIssueCsrfToken(this IEndpointRouteBuilder group)
    {
        group.MapPublicGet("/csrf", HandleAsync)
            .WithName("Identity.Auth.IssueCsrfToken")
            .WithTags("Identity.Auth")
            .WithSummary("Issue a CSRF token for browser clients")
            .WithDescription(
                "Generates a cryptographically random CSRF token, sets it as the " +
                "csrf_token cookie, and returns the same value in the response body. " +
                "Browser clients must echo the body value in the X-CSRF-Token header " +
                "on state-changing requests (ADR-005).")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static IResult HandleAsync(HttpContext httpContext, CsrfProtector protector)
    {
        var token = protector.GenerateToken();
        protector.SetCookie(httpContext, token);
        return Results.Ok(new CsrfTokenResponse(token));
    }

    public sealed record CsrfTokenResponse(string Token);
}
