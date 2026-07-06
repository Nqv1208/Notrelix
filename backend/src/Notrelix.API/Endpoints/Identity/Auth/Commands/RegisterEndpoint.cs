using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegister(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/register", HandleAsync)
            .WithName("Identity.Auth.Register")
            .WithTags("Identity.Auth")
            .WithSummary("Register a new account")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        RegisterRequest request,
        ISender sender,
        ICookieService cookieService)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            Name = request.Name
        };

        var result = await sender.Send(command);

        if (result.Succeeded && result.Data is not null)
        {
            cookieService.SetTokenCookie(result.Data.AccessToken, result.Data.RefreshToken);
        }

        return result.ToApiResult();
    }
}
