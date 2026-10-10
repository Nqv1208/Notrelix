using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyBoardItems;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListMyWorkItemsEndpoint
{
    public static IEndpointRouteBuilder MapListMyWorkItems(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("WorkManagement.BoardItems.ListMyWork")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List open Items assigned to the current user in a workspace")
            .WithDescription(
                "Cursor-paginated (default 50, max 100). Ordered by due date, earliest first and undated last, " +
                "so the client can group items into due windows. Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<MyWorkItemDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyBoardItemsQuery(workspaceId, cursor, limit), cancellationToken);
        return result.ToApiResult();
    }
}
