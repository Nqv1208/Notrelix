using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/login", HandleAsync)
            .WithName("Identity.Auth.Login")
            .WithTags("Identity.Auth")
            .WithSummary("Login with email and password")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        LoginRequest request,
        ISender sender,
        ICookieService cookieService)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var result = await sender.Send(command);

        if (result.Succeeded && result.Data is { MfaRequired: false, AccessToken: not null } auth)
        {
            cookieService.SetTokenCookie(auth.AccessToken, auth.RefreshToken!);
        }

        return result.ToApiResult();
    }
}
