using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.DeleteSpace;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class DeleteSpaceEndpoint
{
    public static IEndpointRouteBuilder MapDeleteSpace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/", HandleAsync)
            .WithName("Workspaces.Spaces.DeleteSpace")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Delete a space (restorable for 30 days)");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        [FromQuery] long expectedVersion,
        ISender sender)
    {
        var result = await sender.Send(new DeleteSpaceCommand(workspaceId, spaceId, expectedVersion));
        return result.ToNoContentResult();
    }
}
