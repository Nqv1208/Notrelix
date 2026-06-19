using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.Logout;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class LogoutEndpoint
{
    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder group)
    {
        group.MapPost("/logout", HandleAsync)
            .WithName("Identity.Auth.Logout")
            .WithTags("Identity.Auth")
            .WithSummary("Logout and revoke refresh token");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        ISender sender,
        ICookieService cookieService)
    {
        var refreshToken = httpContext.Request.Cookies["refreshToken"] ?? string.Empty;
        var accessToken = httpContext.Request.Cookies["accessToken"];

        var command = new LogoutCommand
        {
            RefreshToken = refreshToken,
            AccessToken = accessToken
        };

        var result = await sender.Send(command);

        cookieService.DeleteTokenCookie();

        return result.ToApiResult();
    }
}
