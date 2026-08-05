using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class DuplicateBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapDuplicateBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/duplicate", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.Duplicate")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Duplicate a board item in its current group");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DuplicateBoardItemCommand(itemId), cancellationToken);
        return result.ToCreatedResult();
    }
}
