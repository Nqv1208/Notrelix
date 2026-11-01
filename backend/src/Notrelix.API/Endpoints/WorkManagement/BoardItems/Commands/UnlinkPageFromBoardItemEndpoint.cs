using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UnlinkPageFromBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapUnlinkPageFromBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/link-page/{pageId:guid}", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.UnlinkPage")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Unlink one page from a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid pageId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnlinkPageFromBoardItemCommand(itemId, pageId), cancellationToken);
        return result.ToNoContentResult();
    }
}
