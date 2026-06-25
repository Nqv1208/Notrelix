using MediatR;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder group)
    {
        group.MapPost("/login", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity.Auth.Login")
            .WithTags("Identity.Auth")
            .WithSummary("Login with email and password")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        LoginCommand command,
        ISender sender,
        ICookieService cookieService)
    {
        var result = await sender.Send(command);

        if (result.Succeeded && result.Data is not null)
        {
            cookieService.SetTokenCookie(result.Data.AccessToken, result.Data.RefreshToken);
        }

        return result.ToApiResult();
    }
}
