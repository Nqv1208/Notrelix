using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
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
        UpdateBoardRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardCommand(
            boardId, body.Title, body.Description, body.Background, body.Visibility, body.ExpectedVersion, body.ItemKeyPrefix), cancellationToken);
        return result.ToApiResult();
    }
}
