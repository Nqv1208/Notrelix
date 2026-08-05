using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UpdateBoardItemStatusEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardItemStatus(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/status", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.UpdateStatus")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Update the status of a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        UpdateBoardItemStatusRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardItemStatusCommand(itemId, body.Status), cancellationToken);
        return result.ToNoContentResult();
    }
}
