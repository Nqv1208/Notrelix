using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetHomeFeed;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class GetHomeFeedEndpoint
{
    public static IEndpointRouteBuilder MapGetHomeFeed(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("WorkManagement.BoardItems.GetHomeFeed")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Get what waits for the current user on Home")
            .WithDescription(
                "Unread mentions of Items the caller can still read (replies, newest first, at most 20) with the " +
                "counts of the Home header. Approval replies, events and the pending-approval count are empty: no source exists yet.")
            .Produces<HomeFeedDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetHomeFeedQuery(workspaceId), cancellationToken);
        return result.ToApiResult();
    }
}
