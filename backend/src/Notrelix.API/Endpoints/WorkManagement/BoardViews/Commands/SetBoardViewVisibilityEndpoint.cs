using Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetBoardViewVisibility;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class SetBoardViewVisibilityEndpoint
{
    public static IEndpointRouteBuilder MapSetBoardViewVisibility(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/{viewId:guid}/visibility", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardViews.SetVisibility")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Share a board view with the board's members or keep it private");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        SetBoardViewVisibilityRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetBoardViewVisibilityCommand(boardId, viewId, body.Visibility, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
