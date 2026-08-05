using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardVisibility;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class UpdateBoardVisibilityEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardVisibility(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/visibility", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.UpdateVisibility")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Update board visibility");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        UpdateBoardVisibilityRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardVisibilityCommand(boardId, body.Visibility), cancellationToken);
        return result.ToNoContentResult();
    }
}
