using Notrelix.API.Contracts.Workspaces.Workspaces.Requests;
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
        UpdateWorkspaceProfileRequest request,
        ISender sender)
    {
        var command = new UpdateWorkspaceProfileCommand(
            workspaceId,
            request.Name,
            request.Description,
            request.ExpectedVersion);

        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}
