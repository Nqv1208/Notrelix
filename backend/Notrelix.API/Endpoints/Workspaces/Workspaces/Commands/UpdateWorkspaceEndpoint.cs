using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class UpdateWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapUpdateWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/", HandleAsync)
            .WithName("Workspaces.Workspaces.UpdateWorkspace")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Update workspace settings");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        UpdateWorkspaceCommand command,
        ISender sender)
    {
        var result = await sender.Send(command with { WorkspaceId = workspaceId });
        return result.ToApiResult();
    }
}
