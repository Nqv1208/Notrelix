using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetUserWorkspaces;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class ListUserWorkspacesEndpoint
{
    public static IEndpointRouteBuilder MapListUserWorkspaces(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Workspaces.Workspaces.ListUserWorkspaces")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Get current user's workspaces");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ISender sender,
        ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            return Results.Unauthorized();

        var result = await sender.Send(new GetUserWorkspacesQuery(currentUser.UserId));
        return result.ToApiResult();
    }
}
