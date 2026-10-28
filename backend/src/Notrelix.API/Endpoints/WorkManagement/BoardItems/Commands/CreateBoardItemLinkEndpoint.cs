using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class CreateBoardItemLinkEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoardItemLink(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/links", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.CreateLink")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Link a board item to another item of its board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        CreateBoardItemLinkRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBoardItemLinkCommand(itemId, body.ItemId, body.Relation), cancellationToken);
        return result.ToApiResult();
    }
}
