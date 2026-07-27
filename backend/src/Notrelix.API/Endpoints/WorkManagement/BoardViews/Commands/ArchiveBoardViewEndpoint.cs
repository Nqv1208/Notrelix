using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.ArchiveBoardView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class ArchiveBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapArchiveBoardView(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{viewId:guid}/archive", HandleAsync)
            .WithName("WorkManagement.BoardViews.Archive")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Archive a board view");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveBoardViewCommand(boardId, viewId), cancellationToken);
        return result.ToNoContentResult();
    }
}
