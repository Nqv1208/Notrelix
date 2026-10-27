using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.DeleteBoardItemLink;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class DeleteBoardItemLinkEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBoardItemLink(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/links/{linkId:guid}", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.DeleteLink")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Remove a link of a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid linkId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBoardItemLinkCommand(itemId, linkId), cancellationToken);
        return result.ToNoContentResult();
    }
}
