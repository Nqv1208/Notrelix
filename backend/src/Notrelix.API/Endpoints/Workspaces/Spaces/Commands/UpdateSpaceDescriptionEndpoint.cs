using Notrelix.API.Contracts.Workspaces.Spaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.UpdateSpaceDescription;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class UpdateSpaceDescriptionEndpoint
{
    public static IEndpointRouteBuilder MapUpdateSpaceDescription(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/description", HandleAsync)
            .WithName("Workspaces.Spaces.UpdateSpaceDescription")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Update a space's description");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        UpdateSpaceDescriptionRequest request,
        ISender sender)
    {
        var result = await sender.Send(new UpdateSpaceDescriptionCommand(workspaceId, spaceId, request.Description));
        return result.ToNoContentResult();
    }
}
