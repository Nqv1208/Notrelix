using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateEmail;

namespace Notrelix.API.Endpoints.Identity.Profile.Commands;

public static class UpdateEmailEndpoint
{
    public static IEndpointRouteBuilder MapUpdateEmail(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/email", HandleAsync)
            .WithName("Identity.Profile.UpdateEmail")
            .Produces<UpdateEmailResultDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed)
            .WithTags("Identity.Profile")
            .WithSummary("Update the current user's email address")
            .WithDescription("Requires a single-use step-up proof for the ChangeSecurityIdentity purpose when the address changes.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        UpdateEmailCommand command,
        ISender sender)
        => (await sender.Send(command)).ToApiResult();
}
