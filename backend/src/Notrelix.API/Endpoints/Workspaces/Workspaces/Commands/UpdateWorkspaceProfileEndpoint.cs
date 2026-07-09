using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class UpdateWorkspaceProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpdateWorkspaceProfile(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePatch("/profile", HandleAsync)
            .WithName("Workspaces.Workspaces.UpdateWorkspaceProfile")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Update workspace name and description");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        UpdateWorkspaceProfileCommand command,
        ISender sender)
    {
        var result = await sender.Send(command with { WorkspaceId = workspaceId });
        return result.ToApiResult();
    }
}
