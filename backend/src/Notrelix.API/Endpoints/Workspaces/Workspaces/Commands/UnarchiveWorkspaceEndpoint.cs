using Notrelix.API.Contracts.Workspaces.Workspaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UnarchiveWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class UnarchiveWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapUnarchiveWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/archive/unarchive", HandleAsync)
            .WithName("Workspaces.Workspaces.UnarchiveWorkspace")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Unarchive a previously archived workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ArchiveWorkspaceRequest request,
        ISender sender)
    {
        var result = await sender.Send(new UnarchiveWorkspaceCommand(workspaceId, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
