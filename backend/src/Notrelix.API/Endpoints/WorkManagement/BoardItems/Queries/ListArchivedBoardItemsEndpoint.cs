using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListArchivedBoardItems;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListArchivedBoardItemsEndpoint
{
    public static IEndpointRouteBuilder MapListArchivedBoardItems(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/archived", HandleAsync)
            .WithName("WorkManagement.BoardItems.Archived")
            .Produces<List<ArchivedBoardItemEntryDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List the archived items of a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        [FromQuery] string? q,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListArchivedBoardItemsQuery(boardId, q), cancellationToken);
        return Results.Ok(result);
    }
}
