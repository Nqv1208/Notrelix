using Notrelix.API.Endpoints.Automation.Executions.Queries;

namespace Notrelix.API.Endpoints.Automation.Executions;

public static class MapExecutionEndpoints
{
    public static IEndpointRouteBuilder MapExecutionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/automations")
            .WithTags("Automation.Executions")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapListAutomationExecutions();

        return app;
    }
}
