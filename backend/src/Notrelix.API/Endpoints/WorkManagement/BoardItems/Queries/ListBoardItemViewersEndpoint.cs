using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemViewers;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class ListBoardItemViewersEndpoint
{
    public static IEndpointRouteBuilder MapListBoardItemViewers(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/viewers", HandleAsync)
            .WithName("WorkManagement.BoardItems.ListViewers")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("List who opened a board item and when");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardItemViewersQuery(itemId), cancellationToken);
        return result.ToApiResult();
    }
}
