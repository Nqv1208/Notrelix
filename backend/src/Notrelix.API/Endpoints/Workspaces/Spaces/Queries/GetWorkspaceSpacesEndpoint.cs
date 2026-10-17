using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetWorkspaceSpaces;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

public static class GetWorkspaceSpacesEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceSpaces(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Spaces.GetWorkspaceSpaces")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Get all spaces in a workspace")
            .Produces<List<SpaceDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceSpacesQuery(workspaceId));
        return result.ToApiResult();
    }
}
