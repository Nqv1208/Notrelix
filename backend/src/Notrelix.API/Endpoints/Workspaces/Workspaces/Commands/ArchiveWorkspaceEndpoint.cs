using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class ArchiveWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapArchiveWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/archive", HandleAsync)
            .WithName("Workspaces.Workspaces.ArchiveWorkspace")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Archive (soft delete) a workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        long expectedVersion,
        ISender sender)
    {
        var result = await sender.Send(new ArchiveWorkspaceCommand(workspaceId, expectedVersion));
        return result.ToNoContentResult();
    }
}
