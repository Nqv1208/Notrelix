using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UnlinkPageFromBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapUnlinkPageFromBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/link-page", HandleAsync)
            .WithName("WorkManagement.BoardItems.UnlinkPage")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Unlink page from board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnlinkPageFromBoardItemCommand(itemId), cancellationToken);
        return result.ToNoContentResult();
    }
}
