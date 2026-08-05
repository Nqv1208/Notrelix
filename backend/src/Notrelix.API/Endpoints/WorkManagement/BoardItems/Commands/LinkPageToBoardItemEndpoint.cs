using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.LinkPageToBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class LinkPageToBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapLinkPageToBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/link-page", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.LinkPage")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Link a page to this board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        LinkPageToBoardItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LinkPageToBoardItemCommand(itemId, body.PageId), cancellationToken);
        return result.ToApiResult();
    }
}

