namespace Notrelix.API.Endpoints.Admin;

public static class OutboxDiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapOutboxDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/admin/outbox")
            .WithTags("Admin")
            .RequireAuthorization("SystemAdmin")
            .WithOpenApi();

        group.MapGet("/stats", GetStats)
            .WithName("GetOutboxStats")
            .WithSummary("Outbox message statistics by status");

        group.MapGet("/pending", GetPending)
            .WithName("GetPendingOutboxMessages")
            .WithSummary("Recent pending/processing outbox messages");

        group.MapGet("/failed", GetFailed)
            .WithName("GetFailedOutboxMessages")
            .WithSummary("Failed and dead-letter outbox messages");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetOutboxMessageById")
            .WithSummary("Get a specific outbox message by ID");

        return app;
    }

    private static async Task<IResult> GetStats(
        [FromServices] IOutboxDiagnosticsService diagnostics,
        CancellationToken cancellationToken)
    {
        var stats = await diagnostics.GetStatsAsync(cancellationToken);
        return Results.Ok(stats);
    }

    private static async Task<IResult> GetPending(
        [FromServices] IOutboxDiagnosticsService diagnostics,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await diagnostics.GetPendingAsync(limit, cancellationToken);
        return Results.Ok(messages);
    }

    private static async Task<IResult> GetFailed(
        [FromServices] IOutboxDiagnosticsService diagnostics,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await diagnostics.GetFailedAsync(limit, cancellationToken);
        return Results.Ok(messages);
    }

    private static async Task<IResult> GetById(
        Guid id,
        [FromServices] IOutboxDiagnosticsService diagnostics,
        CancellationToken cancellationToken = default)
    {
        var message = await diagnostics.GetByIdAsync(id, cancellationToken);
        return message is not null ? Results.Ok(message) : Results.NotFound();
    }
}
