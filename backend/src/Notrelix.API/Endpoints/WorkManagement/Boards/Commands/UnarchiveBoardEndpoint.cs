using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UnarchiveBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class UnarchiveBoardEndpoint
{
    public static IEndpointRouteBuilder MapUnarchiveBoard(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/unarchive", HandleUnarchiveBoard)
            .WithName("WorkManagement.Boards.Unarchive");
        return group;
    }

    private static async Task<IResult> HandleUnarchiveBoard(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveBoardCommand(boardId), cancellationToken);
        return result.ToNoContentResult();
    }
}
