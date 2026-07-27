using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.UnarchiveBoardView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class UnarchiveBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapUnarchiveBoardView(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{viewId:guid}/unarchive", HandleAsync)
            .WithName("WorkManagement.BoardViews.Unarchive")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Unarchive a board view");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveBoardViewCommand(boardId, viewId), cancellationToken);
        return result.ToNoContentResult();
    }
}
