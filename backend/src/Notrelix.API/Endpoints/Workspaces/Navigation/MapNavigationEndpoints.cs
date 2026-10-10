using Notrelix.API.Endpoints.Workspaces.Navigation.Commands;
using Notrelix.API.Endpoints.Workspaces.Navigation.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Navigation;

/// <summary>
/// Personal navigation state of the current Workspace Member (WX-BE-001 recents,
/// WX-BE-002 pins; WX-DEC-011). Every route reads or writes only the caller's
/// own entries.
/// </summary>
public static class MapNavigationEndpoints
{
    public static IEndpointRouteBuilder RegisterWorkspaceNavigationEndpoints(this IEndpointRouteBuilder app)
    {
        var recents = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/recents")
            .WithTags("Workspaces.Navigation")
            .WithOpenApi();

        recents.MapGetRecentResources();
        recents.MapRecordRecentResource();

        var pins = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/pins")
            .WithTags("Workspaces.Navigation")
            .WithOpenApi();

        pins.MapGetPinnedResources();
        pins.MapPinResource();
        pins.MapUnpinResource();

        return app;
    }
}
