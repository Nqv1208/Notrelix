using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardSharing;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class GetBoardSharingEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardSharing(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/sharing", HandleAsync)
            .WithName("WorkManagement.Boards.GetSharing")
            .Produces<BoardSharingDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Get who holds access on a board, its sharing log and who would lose it on Private")
            .WithDescription("Entries (people and Teams, level, how it is held) are returned only to a board manager or a board member; any other reader of the board gets the counts only. The log and the Private impact are for board managers. An entry id is the person's user id or the Team's id. Email is shown only to a workspace Owner or Admin and only for non-Guest people.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardSharingQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
