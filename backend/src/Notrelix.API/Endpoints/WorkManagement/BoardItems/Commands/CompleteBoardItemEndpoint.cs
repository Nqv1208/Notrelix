using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CompleteBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class CompleteBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapCompleteBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/complete", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.Complete")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Complete a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        CompleteBoardItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteBoardItemCommand(itemId, body.CompletedAt), cancellationToken);
        return result.ToNoContentResult();
    }
}
