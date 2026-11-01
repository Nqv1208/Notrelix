using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardDependencies;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListBoardDependenciesEndpoint
{
    public static IEndpointRouteBuilder MapListBoardDependencies(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/dependencies", HandleAsync)
            .WithName("WorkManagement.BoardItems.ListDependencies")
            .Produces<BoardDependencyListDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List the dependencies between the items of a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardDependenciesQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
