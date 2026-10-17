using Notrelix.API.Contracts.Workspaces.Spaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.MoveSpace;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class MoveSpaceEndpoint
{
    public static IEndpointRouteBuilder MapMoveSpace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/move", HandleAsync)
            .WithName("Workspaces.Spaces.MoveSpace")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Move a space (with its subtree) under another parent or to the root, at a sibling position");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        MoveSpaceRequest request,
        ISender sender)
    {
        var result = await sender.Send(new MoveSpaceCommand(
            workspaceId, spaceId, request.NewParentSpaceId, request.PreviousSiblingId, request.NextSiblingId, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
