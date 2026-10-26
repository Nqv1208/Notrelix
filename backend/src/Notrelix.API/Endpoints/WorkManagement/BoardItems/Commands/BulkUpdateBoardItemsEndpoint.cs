using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.BulkUpdateBoardItems;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class BulkUpdateBoardItemsEndpoint
{
    public static IEndpointRouteBuilder MapBulkUpdateBoardItems(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/bulk", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.BulkUpdate")
            .Produces<BulkUpdateBoardItemsResult>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Change the status, due date, assignee or archive state of several items of a board at once");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        BulkUpdateBoardItemsRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new BulkUpdateBoardItemsCommand(
            boardId,
            body.Operation,
            body.ItemIds,
            body.Status,
            body.DueDate,
            body.AssigneeUserId), cancellationToken);
        return Results.Ok(result);
    }
}
