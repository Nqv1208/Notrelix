using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.DeleteBoardView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class DeleteBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBoardView(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/{viewId:guid}", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardViews.Delete")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Delete a board view");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        [FromQuery] long expectedVersion,
        [FromQuery] Guid? newDefaultViewId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBoardViewCommand(boardId, viewId, expectedVersion, newDefaultViewId), cancellationToken);
        return result.ToNoContentResult();
    }
}
