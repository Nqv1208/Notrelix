using Notrelix.API.Endpoints.Analytics.Dashboards.Commands;
using Notrelix.API.Endpoints.Analytics.Placements.Queries;
using Notrelix.API.Endpoints.Analytics.Queries;

namespace Notrelix.API.Endpoints.Analytics;

public static class AnalyticsEndpointMappings
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/boards/{boardId:guid}/report")
            .WithTags("Analytics")
            .WithOpenApi()
            .MapGetBoardReport();

        app.MapGroup("/api/v1/workspaces/{workspaceId:guid}/work-report")
            .WithTags("Analytics")
            .WithOpenApi()
            .MapGetWorkspaceReport();

        app.MapGroup("/api/v1/workspaces/{workspaceId:guid}/work-placements")
            .WithTags("Analytics")
            .WithOpenApi()
            .MapListWorkspacePlacements();

        app.MapGroup("/api/v1/workspaces/{workspaceId:guid}/dashboards")
            .WithTags("Analytics")
            .WithOpenApi()
            .MapCreateDashboard();

        return app;
    }
}
