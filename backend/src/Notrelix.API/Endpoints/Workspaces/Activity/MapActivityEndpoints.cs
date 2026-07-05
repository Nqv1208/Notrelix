using Notrelix.API.Endpoints.Workspaces.Activity.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Activity;

public static class MapActivityEndpoints
{
    public static IEndpointRouteBuilder RegisterWorkspaceActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/activity")
            .WithTags("Workspaces.Activity")
            .WithOpenApi();

        group.MapGetWorkspaceActivity();

        return app;
    }
}
