using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.ClearBoardItemLinks;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class ClearBoardItemLinksEndpoint
{
    public static IEndpointRouteBuilder MapClearBoardItemLinks(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/links", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.ClearLinks")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Remove every link of a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ClearBoardItemLinksCommand(itemId), cancellationToken);
        return result.ToApiResult();
    }
}
