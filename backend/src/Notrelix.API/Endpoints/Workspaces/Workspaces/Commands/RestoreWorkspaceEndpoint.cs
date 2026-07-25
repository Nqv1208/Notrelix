using Notrelix.API.Contracts.Workspaces.Workspaces.Requests;
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
        RestoreWorkspaceRequest request,
        ISender sender)
    {
        var result = await sender.Send(new RestoreWorkspaceCommand(workspaceId, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
