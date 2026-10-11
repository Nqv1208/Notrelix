using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

public static class GetSpaceTreeEndpoint
{
    public static IEndpointRouteBuilder MapGetSpaceTree(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/tree", HandleAsync)
            .WithName("Workspaces.Spaces.GetSpaceTree")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Get the caller's visible space org tree as a parent-linked list")
            .Produces<List<SpaceTreeNodeDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetSpaceTreeQuery(workspaceId));
        return result.ToApiResult();
    }
}
