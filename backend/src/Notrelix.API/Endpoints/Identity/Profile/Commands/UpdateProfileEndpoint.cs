using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;

namespace Notrelix.API.Endpoints.Identity.Profile.Commands;

public static class UpdateProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpdateProfile(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/", HandleAsync)
            .WithName("Identity.Profile.UpdateProfile")
            .WithTags("Identity.Profile")
            .WithSummary("Update current user profile");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        UpdateProfileCommand command,
        ISender sender,
        ICurrentUser currentUser)
    {
        var request = command with { UserId = currentUser.UserId };
        var result = await sender.Send(request);
        return result.ToApiResult();
    }
}
