using Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;
using Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces;

public static class MapWorkspaceEndpoints
{
    public static IEndpointRouteBuilder RegisterWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces")
            .WithTags("Workspaces.Workspaces")
            .WithOpenApi();

        group.MapListUserWorkspaces();

        var byIdGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}")
            .WithTags("Workspaces.Workspaces")
            .WithOpenApi();

        byIdGroup.MapGetWorkspace();
        byIdGroup.MapUpdateWorkspaceProfile();
        byIdGroup.MapArchiveWorkspace();
        byIdGroup.MapUnarchiveWorkspace();
        byIdGroup.MapRestoreWorkspace();
        byIdGroup.MapDeleteWorkspace();
        byIdGroup.MapTransferOwnership();

        var accountGroup = app
            .MapGroup("/api/v1/accounts/{accountId:guid}/workspaces")
            .WithTags("Workspaces.Workspaces")
            .WithOpenApi();

        accountGroup.MapCreateWorkspace();
        accountGroup.MapGetAccountWorkspaces();

        app.MapResolveSlug();

        return app;
    }
}
