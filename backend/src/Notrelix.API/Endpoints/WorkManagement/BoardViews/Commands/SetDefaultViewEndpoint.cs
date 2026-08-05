using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetDefaultView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class SetDefaultViewEndpoint
{
    public static IEndpointRouteBuilder MapSetDefaultView(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{viewId:guid}/set-default", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardViews.SetDefault")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Set a board view as the default view");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetDefaultViewCommand(boardId, viewId), cancellationToken);
        return result.ToNoContentResult();
    }
}
