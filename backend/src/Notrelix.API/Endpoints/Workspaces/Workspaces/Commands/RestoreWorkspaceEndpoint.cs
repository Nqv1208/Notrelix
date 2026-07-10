using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class RestoreWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapRestoreWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/restore", HandleAsync)
            .WithName("Workspaces.Workspaces.RestoreWorkspace")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Restore a soft-deleted workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        long expectedVersion,
        ISender sender)
    {
        var result = await sender.Send(new RestoreWorkspaceCommand(workspaceId, expectedVersion));
        return result.ToNoContentResult();
    }
}
