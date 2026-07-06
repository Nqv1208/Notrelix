using Notrelix.API.Endpoints.Collaboration.Activity.Queries;

namespace Notrelix.API.Endpoints.Collaboration.Activity;

public static class MapActivityEndpoints
{
    public static IEndpointRouteBuilder RegisterBoardItemActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/board-items/{boardItemId:guid}/activity")
            .WithTags("Collaboration.Activity")
            .WithOpenApi();

        group.MapGetBoardItemActivity();

        return app;
    }
}
