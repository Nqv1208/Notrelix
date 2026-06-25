using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class GetBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("WorkManagement.BoardItems.Get")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Get board item detail with members, labels, checklists");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardItemQuery(itemId), cancellationToken);
        return result.ToApiResult();
    }
}
