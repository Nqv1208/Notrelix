using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetDeletedSpaces;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

public static class GetDeletedSpacesEndpoint
{
    public static IEndpointRouteBuilder MapGetDeletedSpaces(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/deleted", HandleAsync)
            .WithName("Workspaces.Spaces.GetDeletedSpaces")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Get the deleted spaces that can still be restored (30 days), for a workspace Owner or Admin")
            .Produces<List<DeletedSpaceDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetDeletedSpacesQuery(workspaceId));
        return result.ToApiResult();
    }
}
