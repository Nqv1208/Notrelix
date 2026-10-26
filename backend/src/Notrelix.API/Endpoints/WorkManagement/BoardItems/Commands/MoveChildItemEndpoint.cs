using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveChildItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class MoveChildItemEndpoint
{
    public static IEndpointRouteBuilder MapMoveChildItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/move-within-parent", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.MoveChild")
            .Produces<ChildItemDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Place a sub-item among the sub-items of its parent");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        MoveChildItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveChildItemCommand(itemId, body.ExpectedVersion, body.PreviousSiblingId, body.NextSiblingId), cancellationToken);
        return Results.Ok(result);
    }
}
