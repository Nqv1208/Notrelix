using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DeleteBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class DeleteBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("WorkManagement.BoardItems.Delete")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Delete a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBoardItemCommand(itemId), cancellationToken);
        return result.ToNoContentResult();
    }
}
