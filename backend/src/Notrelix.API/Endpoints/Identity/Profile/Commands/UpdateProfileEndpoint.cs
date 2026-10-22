using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;

namespace Notrelix.API.Endpoints.Identity.Profile.Commands;

public static class UpdateProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpdateProfile(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPatch("/", HandleAsync)
            .WithName("Identity.Profile.UpdateProfile")
            .Produces<UserDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized)
            .WithTags("Identity.Profile")
            .WithSummary("Update current user profile");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        UpdateProfileCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}
