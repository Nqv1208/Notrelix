using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;

namespace Notrelix.API.Endpoints.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/health")
            .WithTags("Health")
            .AllowAnonymous()
            .WithOpenApi();

        group.MapGet("/", GetHealth)
            .WithName("GetHealth")
            .WithSummary("Full health check with service status");

        group.MapGet("/live", Live)
            .WithName("LivenessProbe")
            .WithSummary("Liveness probe");

        group.MapGet("/ready", Ready)
            .WithName("ReadinessProbe")
            .WithSummary("Readiness probe — checks database connectivity");

        return app;
    }

    private static async Task<IResult> GetHealth(IApplicationDbContext context)
    {
        var health = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Services = new Dictionary<string, object>()
        };

        try
        {
            await context.Users.AnyAsync();
            health.Services["database"] = new { Status = "Healthy", Message = "PostgreSQL connection OK" };
        }
        catch (Exception ex)
        {
            return Results.Json(
                new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Services = new Dictionary<string, object>
                    {
                        ["database"] = new { Status = "Unhealthy", Message = ex.Message }
                    }
                },
                statusCode: 503);
        }

        return Results.Ok(health);
    }

    private static IResult Live()
    {
        return Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    private static async Task<IResult> Ready(IApplicationDbContext context)
    {
        try
        {
            await context.Users.AnyAsync();
            return Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }
        catch
        {
            return Results.Json(
                new { status = "not ready", timestamp = DateTime.UtcNow },
                statusCode: 503);
        }
    }
}
