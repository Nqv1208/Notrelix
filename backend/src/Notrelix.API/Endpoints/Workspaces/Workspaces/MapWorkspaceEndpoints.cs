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
            .RequireAuthorization()
            .WithOpenApi();

        group.MapListUserWorkspaces();
        group.MapCreateWorkspace();

        var byIdGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}")
            .WithTags("Workspaces.Workspaces")
            .RequireAuthorization()
            .WithOpenApi();

        byIdGroup.MapGetWorkspace();
        byIdGroup.MapUpdateWorkspace();
        byIdGroup.MapArchiveWorkspace();

        var bySlugGroup = app
            .MapGroup("/api/v1/workspaces/by-slug/{slug}")
            .WithTags("Workspaces.Workspaces")
            .RequireAuthorization()
            .WithOpenApi();

        bySlugGroup.MapGetWorkspaceBySlug();

        return app;
    }
}
