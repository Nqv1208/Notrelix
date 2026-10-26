using Notrelix.API.Endpoints.Workspaces.Search.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Search;

/// <summary>The Workspace-scoped command search (ADR-010): one read over what the caller may open.</summary>
public static class MapSearchEndpoints
{
    public static IEndpointRouteBuilder RegisterWorkspaceSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/command-search")
            .WithTags("Workspaces.Search")
            .WithOpenApi();

        group.MapCommandSearch();

        return app;
    }
}
