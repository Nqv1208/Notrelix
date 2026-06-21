using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class ArchiveBoardEndpoint
{
    public static IEndpointRouteBuilder MapArchiveBoard(this IEndpointRouteBuilder group)
    {
        group.MapPost("/archive", HandleAsync)
            .WithName("WorkManagement.Boards.Archive")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Archive a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveBoardCommand(boardId), cancellationToken);
        return result.ToNoContentResult();
    }
}
