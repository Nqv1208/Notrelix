using MediatR;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class RefreshTokenEndpoint
{
    public static IEndpointRouteBuilder MapRefreshToken(this IEndpointRouteBuilder group)
    {
        group.MapPost("/refresh", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity.Auth.RefreshToken")
            .WithTags("Identity.Auth")
            .WithSummary("Refresh the access token")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        ISender sender,
        ICookieService cookieService)
    {
        var refreshToken = httpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Results.Unauthorized();
        }

        var command = new RefreshTokenCommand { RefreshToken = refreshToken };
        var result = await sender.Send(command);
        if (result.Succeeded && result.Data is not null)
        {
            cookieService.SetTokenCookie(result.Data.AccessToken, result.Data.RefreshToken);
        }

        return result.ToApiResult();
    }
}
