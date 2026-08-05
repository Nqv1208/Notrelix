using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RestoreBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class RestoreBoardEndpoint
{
    public static IEndpointRouteBuilder MapRestoreBoard(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/restore", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.Restore")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Restore a soft-deleted board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreBoardCommand(boardId), cancellationToken);
        return result.ToNoContentResult();
    }
}
