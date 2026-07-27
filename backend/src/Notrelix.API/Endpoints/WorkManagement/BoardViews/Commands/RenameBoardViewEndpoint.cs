using Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.RenameBoardView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class RenameBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapRenameBoardView(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/{viewId:guid}/rename", HandleAsync)
            .WithName("WorkManagement.BoardViews.Rename")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Rename a board view");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        RenameBoardViewRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RenameBoardViewCommand(viewId, body.Name), cancellationToken);
        return result.ToNoContentResult();
    }
}
