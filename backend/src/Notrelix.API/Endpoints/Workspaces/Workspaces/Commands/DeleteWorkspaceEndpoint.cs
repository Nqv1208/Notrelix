using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class DeleteWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapDeleteWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/", HandleAsync)
            .WithName("Workspaces.Workspaces.DeleteWorkspace")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Permanently delete a workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender,
        [FromQuery] long expectedVersion)
    {
        var result = await sender.Send(new DeleteWorkspaceCommand(workspaceId, expectedVersion));
        return result.ToNoContentResult();
    }
}
