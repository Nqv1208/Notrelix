using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.MoveBoardToSpace;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class MoveBoardToSpaceEndpoint
{
    public static IEndpointRouteBuilder MapMoveBoardToSpace(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/move-to-space", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.MoveToSpace")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Move a board into a space or back to the workspace root");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        MoveBoardToSpaceRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveBoardToSpaceCommand(boardId, body.SpaceId, body.ExpectedVersion, body.AddUserIds), cancellationToken);
        return result.ToNoContentResult();
    }
}
