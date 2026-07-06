using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class GetBoardOverviewEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardOverview(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/full", HandleAsync)
            .WithName("WorkManagement.Boards.GetOverview")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Get full board with lists and cards");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFullBoardQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
