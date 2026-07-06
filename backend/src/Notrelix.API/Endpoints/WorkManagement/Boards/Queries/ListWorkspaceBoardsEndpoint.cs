using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class ListWorkspaceBoardsEndpoint
{
    public static IEndpointRouteBuilder MapListWorkspaceBoards(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("WorkManagement.Boards.ListWorkspaceBoards")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Get all boards in a workspace by workspace ID");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardsQuery(workspaceId), cancellationToken);
        return result.ToApiResult();
    }
}
