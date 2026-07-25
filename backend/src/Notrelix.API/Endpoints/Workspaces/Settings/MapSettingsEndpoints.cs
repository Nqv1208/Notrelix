using Notrelix.API.Endpoints.Workspaces.Settings.Commands;
using Notrelix.API.Endpoints.Workspaces.Settings.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Settings;

public static class MapSettingsEndpoints
{
    public static IEndpointRouteBuilder RegisterSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/settings")
            .WithTags("Workspaces.Settings")
            .WithOpenApi();

        group.MapGetWorkspaceSettings();
        group.MapUpdateWorkspaceSettings();

        return app;
    }
}
