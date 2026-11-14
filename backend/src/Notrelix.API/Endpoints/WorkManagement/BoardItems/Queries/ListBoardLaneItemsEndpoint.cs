using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardLaneItems;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListBoardLaneItemsEndpoint
{
    public static IEndpointRouteBuilder MapListBoardLaneItems(this IEndpointRouteBuilder group)
    {
        // Mapped on the board's items group, which is rooted at /boards/{boardId}/items.
        group.MapResourceGet("/lanes/{lane}", HandleAsync)
            .WithName("WorkManagement.BoardItems.ListLane")
            .Produces<BoardLaneItemPageDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Get one page of a lane's items under a filter, sort and grouping");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        string lane,
        string? cursor,
        int? limit,
        string? filter,
        string? sort,
        string? q,
        string? qMembers,
        string? groupBy,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var page = await sender.Send(new ListBoardLaneItemsQuery(boardId, lane, cursor, limit, filter, sort, q, qMembers, groupBy), cancellationToken);
        return Results.Ok(page);
    }
}
