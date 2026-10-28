using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Queries.ListBoardViews;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Queries;

public static class ListBoardViewsEndpoint
{
    public static IEndpointRouteBuilder MapListBoardViews(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/saved", HandleAsync)
            .WithName("WorkManagement.BoardViews.List")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("List the saved views the caller sees on a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardViewsQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
