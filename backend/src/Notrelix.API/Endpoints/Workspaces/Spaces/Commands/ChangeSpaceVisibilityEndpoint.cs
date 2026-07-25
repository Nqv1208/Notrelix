using Notrelix.API.Contracts.Workspaces.Spaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceVisibility;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class ChangeSpaceVisibilityEndpoint
{
    public static IEndpointRouteBuilder MapChangeSpaceVisibility(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/visibility", HandleAsync)
            .WithName("Workspaces.Spaces.ChangeSpaceVisibility")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Change a space's visibility");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        ChangeSpaceVisibilityRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ChangeSpaceVisibilityCommand(workspaceId, spaceId, request.Visibility));
        return result.ToNoContentResult();
    }
}
