using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class ArchiveSpaceEndpoint
{
    public static IEndpointRouteBuilder MapArchiveSpace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/archive", HandleAsync)
            .WithName("Workspaces.Spaces.ArchiveSpace")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Archive a space");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        ISender sender)
    {
        var result = await sender.Send(new ArchiveSpaceCommand(workspaceId, spaceId));
        return result.ToNoContentResult();
    }
}
