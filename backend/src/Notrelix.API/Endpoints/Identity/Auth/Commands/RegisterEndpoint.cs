using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegister(this IEndpointRouteBuilder group)
    {
        group.MapPost("/register", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity.Auth.Register")
            .WithTags("Identity.Auth")
            .WithSummary("Register a new account")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        RegisterCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}
