using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItemToBoard;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class MoveBoardItemToBoardEndpoint
{
    public static IEndpointRouteBuilder MapMoveBoardItemToBoard(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/move-to-board", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.MoveToBoard")
            .Produces<MoveBoardItemToBoardResult>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Move a board item to another board of the workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        MoveBoardItemToBoardRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveBoardItemToBoardCommand(itemId, body.BoardId, body.ExpectedVersion), cancellationToken);
        return Results.Ok(result);
    }
}
