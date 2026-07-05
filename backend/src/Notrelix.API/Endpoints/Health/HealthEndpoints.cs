using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notrelix.API.Endpoints.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/health")
            .WithTags("Health")
            .WithOpenApi();

        group.MapPublicGet("/", GetHealth)
            .WithName("GetHealth")
            .WithSummary("Full health check with service status");

        group.MapPublicGet("/live", Live)
            .WithName("LivenessProbe")
            .WithSummary("Liveness probe");

        group.MapInternalGet("/ready", Ready)
            .WithName("ReadinessProbe")
            .WithSummary("Readiness probe — checks database connectivity");

        return app;
    }

    private static async Task<IResult> GetHealth([FromServices] HealthCheckService healthCheck, HttpContext context)
    {
        var report = await healthCheck.CheckHealthAsync();
        return Results.Ok(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration
            })
        });
    }

    private static IResult Live()
    {
        return Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    private static async Task<IResult> Ready([FromServices] HealthCheckService healthCheck)
    {
        try
        {
            var report = await healthCheck.CheckHealthAsync(
                pred => pred.Tags.Contains("ready"));
            return Results.Ok(new { status = report.Status.ToString(), timestamp = DateTime.UtcNow });
        }
        catch
        {
            return Results.Json(
                new { status = "not ready", timestamp = DateTime.UtcNow },
                statusCode: 503);
        }
    }
}
