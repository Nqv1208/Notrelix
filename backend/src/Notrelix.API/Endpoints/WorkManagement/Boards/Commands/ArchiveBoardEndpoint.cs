using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class ArchiveBoardEndpoint
{
    public static IEndpointRouteBuilder MapArchiveBoard(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/archive", HandleArchiveBoard)
            .WithName("WorkManagement.Boards.Archive");
        return group;
    }

    private static async Task<IResult> HandleArchiveBoard(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveBoardCommand(boardId), cancellationToken);
        return result.ToNoContentResult();
    }
}
