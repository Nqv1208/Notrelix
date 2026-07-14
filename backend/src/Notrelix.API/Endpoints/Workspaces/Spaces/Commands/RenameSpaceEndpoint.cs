using Notrelix.API.Contracts.Workspaces.Spaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RenameSpace;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class RenameSpaceEndpoint
{
    public static IEndpointRouteBuilder MapRenameSpace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/", HandleAsync)
            .WithName("Workspaces.Spaces.RenameSpace")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Rename a space");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        RenameSpaceRequest request,
        ISender sender)
    {
        var result = await sender.Send(new RenameSpaceCommand(workspaceId, spaceId, request.Name));
        return result.ToNoContentResult();
    }
}
