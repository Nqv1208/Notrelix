using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class RenameBoardEndpoint
{
    public static IEndpointRouteBuilder MapRenameBoard(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.Rename")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Update board settings");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        UpdateBoardCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var cmd = command with { BoardId = boardId };
        var result = await sender.Send(cmd, cancellationToken);
        return result.ToApiResult();
    }
}
