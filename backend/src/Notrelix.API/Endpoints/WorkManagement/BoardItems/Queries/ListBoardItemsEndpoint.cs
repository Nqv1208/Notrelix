using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItems;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListBoardItemsEndpoint
{
    public static IEndpointRouteBuilder MapListBoardItems(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.BoardItems.List")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Get all items of a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardItemsQuery(boardId), cancellationToken);
        return Results.Ok(result);
    }
}
