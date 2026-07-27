using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UnarchiveBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapUnarchiveBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/unarchive", HandleAsync)
            .WithName("WorkManagement.BoardItems.Unarchive")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Unarchive a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveBoardItemCommand(itemId), cancellationToken);
        return result.ToNoContentResult();
    }
}
