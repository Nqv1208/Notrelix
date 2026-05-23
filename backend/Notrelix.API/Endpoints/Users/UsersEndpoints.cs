using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Identity.Commands.UpdateProfile;

namespace Notrelix.API.Endpoints.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPatch("/profile", UpdateProfile)
            .WithName("UpdateProfile")
            .WithSummary("Update current user profile");

        return app;
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileCommand command,
        ISender sender,
        ICurrentUser currentUser)
    {
        var request = command with { UserId = currentUser.UserId };
        var result = await sender.Send(request);
        return result.ToApiResult();
    }
}
