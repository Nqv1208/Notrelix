using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UnarchiveBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapUnarchiveBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/unarchive", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.Unarchive")
            .Produces<BoardItemVersionDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Unarchive a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        [FromQuery] long expectedVersion,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveBoardItemCommand(itemId, expectedVersion), cancellationToken);
        return result.ToItemVersionResult();
    }
}
