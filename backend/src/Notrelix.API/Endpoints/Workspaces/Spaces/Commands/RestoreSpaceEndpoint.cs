using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RestoreSpace;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class RestoreSpaceEndpoint
{
    public static IEndpointRouteBuilder MapRestoreSpace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/restore", HandleAsync)
            .WithName("Workspaces.Spaces.RestoreSpace")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Restore a soft-deleted space");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        ISender sender)
    {
        var result = await sender.Send(new RestoreSpaceCommand(workspaceId, spaceId));
        return result.ToNoContentResult();
    }
}
