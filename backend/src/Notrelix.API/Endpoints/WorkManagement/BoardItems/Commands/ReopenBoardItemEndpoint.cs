using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ReopenBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class ReopenBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapReopenBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/reopen", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.Reopen")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Reopen a completed board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReopenBoardItemCommand(itemId), cancellationToken);
        return result.ToNoContentResult();
    }
}
