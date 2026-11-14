using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Placements.Queries.GetWorkspacePlacements;

namespace Notrelix.API.Endpoints.Analytics.Placements.Queries;

public static class ListWorkspacePlacementsEndpoint
{
    public static IEndpointRouteBuilder MapListWorkspacePlacements(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Analytics.Placements.ListWorkspacePlacements")
            .WithTags("Analytics")
            .WithSummary("List the work item placements of the Boards the caller can read, as a cursor page")
            .WithDescription(
                "Cursor-paginated (default 50, max 200), in item id order. Only items on Boards the caller may read " +
                "are returned. Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<WorkItemPlacementDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkspacePlacementsQuery(workspaceId, cursor, limit), cancellationToken);
        return result.ToApiResult();
    }
}
