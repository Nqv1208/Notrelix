using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Watchers.Queries.GetBoardItemFollowing;

namespace Notrelix.API.Endpoints.Collaboration.Watchers.Queries;

public static class GetBoardItemFollowingEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardItemFollowing(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("Collaboration.Watchers.GetBoardItemFollowing")
            .WithTags("Collaboration.Watchers")
            .WithSummary("Whether the caller follows a board item, and who does");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid boardItemId, ISender sender)
    {
        var result = await sender.Send(new GetBoardItemFollowingQuery(boardItemId));
        return result.ToApiResult();
    }
}
