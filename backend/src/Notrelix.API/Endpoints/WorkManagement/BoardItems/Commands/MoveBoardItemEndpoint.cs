using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class MoveBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapMoveBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/move", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.Move")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Move board item to another group or change position");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        MoveBoardItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveBoardItemCommand(itemId, body.GroupId, body.Position), cancellationToken);
        return Results.Ok(result);
    }
}

