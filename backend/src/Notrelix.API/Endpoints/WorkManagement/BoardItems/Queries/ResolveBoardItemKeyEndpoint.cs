using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ResolveBoardItemKey;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ResolveBoardItemKeyEndpoint
{
    public static IEndpointRouteBuilder MapResolveBoardItemKey(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/by-key/{key}", HandleAsync)
            .WithName("WorkManagement.BoardItems.ResolveKey")
            .Produces<ResolvedBoardItemKeyDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Open an item by its key on this board, or by the old key it kept after moving to another board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        string key,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResolveBoardItemKeyQuery(boardId, key), cancellationToken);
        return Results.Ok(result);
    }
}
