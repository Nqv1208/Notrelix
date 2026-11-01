using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardMoveImpact;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class GetBoardMoveImpactEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardMoveImpact(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/move-impact", HandleAsync)
            .WithName("WorkManagement.Boards.GetMoveImpact")
            .Produces<BoardMoveImpactDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Who gains and who loses a board if it is placed in another space, and who would be left with unfinished items");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid? targetSpaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardMoveImpactQuery(boardId, targetSpaceId), cancellationToken);
        return result.ToApiResult();
    }
}
