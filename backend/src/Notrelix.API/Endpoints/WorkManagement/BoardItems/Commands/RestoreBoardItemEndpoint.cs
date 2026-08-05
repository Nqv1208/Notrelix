using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RestoreBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class RestoreBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapRestoreBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/restore", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.Restore")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Restore a soft-deleted board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreBoardItemCommand(itemId), cancellationToken);
        return result.ToNoContentResult();
    }
}
