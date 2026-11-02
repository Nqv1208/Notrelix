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
            .Produces(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Register a new account")
            .WithDescription("Answers 200 with no body for a new and for an existing address alike, and sets no session: a new person confirms the email from the mail and signs in; the owner of an existing address gets a notice mail. Nothing in the answer says whether the address was registered.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        RegisterRequest request,
        ISender sender)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            Name = request.Name
        };

        try
        {
            var result = await sender.Send(command);
            return result.ToApiResult();
        }
        catch (ConflictException)
        {
            // The loser of a concurrent registration of one address (the unique index decided): the same
            // neutral answer, the winner's mail already covers the address.
            return Results.Ok();
        }
    }
}
