using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.UnarchiveSpace;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class UnarchiveSpaceEndpoint
{
    public static IEndpointRouteBuilder MapUnarchiveSpace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/unarchive", HandleAsync)
            .WithName("Workspaces.Spaces.UnarchiveSpace")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Unarchive a previously archived space");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        ISender sender)
    {
        var result = await sender.Send(new UnarchiveSpaceCommand(workspaceId, spaceId));
        return result.ToNoContentResult();
    }
}
