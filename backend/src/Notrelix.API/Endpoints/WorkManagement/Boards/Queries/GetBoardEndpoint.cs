using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class GetBoardEndpoint
{
    public static IEndpointRouteBuilder MapGetBoard(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Boards.Get")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Get board by ID");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardQuery(workspaceId, boardId), cancellationToken);
        return result.ToApiResult();
    }
}
