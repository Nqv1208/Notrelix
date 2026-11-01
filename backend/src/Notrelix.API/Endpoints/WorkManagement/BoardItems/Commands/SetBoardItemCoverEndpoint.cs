using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemCover;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class SetBoardItemCoverEndpoint
{
    public static IEndpointRouteBuilder MapSetBoardItemCover(this IEndpointRouteBuilder group)
    {
        group.MapResourcePut("/cover", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.SetCover")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Set or remove the cover of a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        SetBoardItemCoverRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetBoardItemCoverCommand(itemId, body.Color, body.AttachmentId, body.Style, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
