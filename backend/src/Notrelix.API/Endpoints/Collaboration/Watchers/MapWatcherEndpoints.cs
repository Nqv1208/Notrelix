using Notrelix.API.Endpoints.Collaboration.Watchers.Commands;
using Notrelix.API.Endpoints.Collaboration.Watchers.Queries;

namespace Notrelix.API.Endpoints.Collaboration.Watchers;

public static class MapWatcherEndpoints
{
    public static IEndpointRouteBuilder MapWatchersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/board-items/{boardItemId:guid}/following")
            .WithTags("Collaboration.Watchers")
            .WithOpenApi();

        group.MapGetBoardItemFollowing();
        group.MapFollowBoardItem();
        group.MapUnfollowBoardItem();

        return app;
    }
}
