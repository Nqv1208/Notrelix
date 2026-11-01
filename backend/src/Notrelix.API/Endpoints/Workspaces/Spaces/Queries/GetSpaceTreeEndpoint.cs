using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
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
            .WithDescription(
                "Cursor-paginated (default 50, max 200) in space id order; read every page and order siblings by position. " +
                "Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<SpaceTreeNodeDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender)
    {
        var result = await sender.Send(new GetSpaceTreeQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}
