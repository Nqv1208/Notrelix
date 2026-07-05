using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.DeleteBoardView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class DeleteBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBoardView(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/{viewId:guid}", HandleAsync)
            .WithName("WorkManagement.BoardViews.Delete")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Delete a board view");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBoardViewCommand(workspaceId, boardId, viewId), cancellationToken);
        return result.ToNoContentResult();
    }
}
