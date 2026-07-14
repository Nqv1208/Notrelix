using Notrelix.API.Contracts.Workspaces.Spaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceType;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class ChangeSpaceTypeEndpoint
{
    public static IEndpointRouteBuilder MapChangeSpaceType(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/type", HandleAsync)
            .WithName("Workspaces.Spaces.ChangeSpaceType")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Change a space's type");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        ChangeSpaceTypeRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ChangeSpaceTypeCommand(workspaceId, spaceId, request.SpaceType));
        return result.ToNoContentResult();
    }
}
