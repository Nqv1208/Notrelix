using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemTrash;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListBoardItemTrashEndpoint
{
    public static IEndpointRouteBuilder MapListBoardItemTrash(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/trash", HandleAsync)
            .WithName("WorkManagement.BoardItems.Trash")
            .Produces<List<BoardItemTrashEntryDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List the items deleted from a board in the last 30 days");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardItemTrashQuery(boardId), cancellationToken);
        return Results.Ok(result);
    }
}
