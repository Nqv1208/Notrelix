using Notrelix.API.Endpoints.Workspaces.Spaces.Commands;
using Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Spaces;

public static class MapSpaceEndpoints
{
    public static IEndpointRouteBuilder RegisterSpaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/spaces")
            .WithTags("Workspaces.Spaces")
            .WithOpenApi();

        group.MapGetWorkspaceSpaces();
        group.MapCreateSpace();

        var byIdGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/spaces/{spaceId:guid}")
            .WithTags("Workspaces.Spaces")
            .WithOpenApi();

        byIdGroup.MapGetSpace();
        byIdGroup.MapRenameSpace();
        byIdGroup.MapUpdateSpaceDescription();
        byIdGroup.MapChangeSpaceVisibility();
        byIdGroup.MapChangeSpaceType();
        byIdGroup.MapArchiveSpace();
        byIdGroup.MapUnarchiveSpace();
        byIdGroup.MapDeleteSpace();
        byIdGroup.MapRestoreSpace();

        return app;
    }
}
