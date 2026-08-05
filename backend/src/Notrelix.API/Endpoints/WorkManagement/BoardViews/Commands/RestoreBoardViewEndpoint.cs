using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.RestoreBoardView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class RestoreBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapRestoreBoardView(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{viewId:guid}/restore", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardViews.Restore")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Restore a soft-deleted board view");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreBoardViewCommand(viewId), cancellationToken);
        return result.ToNoContentResult();
    }
}
