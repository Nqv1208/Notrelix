using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.DeleteBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class DeleteBoardEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBoard(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleDeleteBoard)
            .WithName("WorkManagement.Boards.Delete");
        return group;
    }

    private static async Task<IResult> HandleDeleteBoard(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBoardCommand(boardId), cancellationToken);
        return result.ToNoContentResult();
    }
}
