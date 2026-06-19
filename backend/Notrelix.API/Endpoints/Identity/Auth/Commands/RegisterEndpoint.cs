using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Commands.Register;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegister(this IEndpointRouteBuilder group)
    {
        group.MapPost("/register", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity.Auth.Register")
            .WithTags("Identity.Auth")
            .WithSummary("Register a new account");
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
