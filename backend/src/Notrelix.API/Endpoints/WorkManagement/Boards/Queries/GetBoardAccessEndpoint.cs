using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardAccess;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class GetBoardAccessEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardAccess(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/access", HandleAsync)
            .WithName("WorkManagement.Boards.GetAccess")
            .Produces<BoardAccessDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Get the caller's access to a board and who can reach it");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardAccessQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
