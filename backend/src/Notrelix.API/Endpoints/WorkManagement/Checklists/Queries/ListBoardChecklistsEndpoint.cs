using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Queries.ListBoardChecklists;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Queries;

public static class ListBoardChecklistsEndpoint
{
    public static IEndpointRouteBuilder MapListBoardChecklists(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Checklists.ListForBoard")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("List the checklists of a board's items, to copy one");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardChecklistsQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
