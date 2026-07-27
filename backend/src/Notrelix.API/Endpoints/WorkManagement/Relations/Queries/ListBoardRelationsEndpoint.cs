using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Relations.Queries.ListBoardRelations;

namespace Notrelix.API.Endpoints.WorkManagement.Relations.Queries;

public static class ListBoardRelationsEndpoint
{
    public static IEndpointRouteBuilder MapListBoardRelations(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Relations.List")
            .WithTags("WorkManagement.Relations")
            .WithSummary("List all relations for a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardRelationsQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
