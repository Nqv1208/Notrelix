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

        return app;
    }
}
