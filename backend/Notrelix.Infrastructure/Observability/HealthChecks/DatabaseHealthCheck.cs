namespace Notrelix.Infrastructure.Observability.HealthChecks;

/// <summary>
/// Skeleton database health check (v4 §18). Real implementation implements
/// <c>IHealthCheck</c> and pings the DB; companion checks cover Redis, outbox
/// backlog, worker heartbeat and storage. Wiring lives in the API host. Not yet wired.
/// </summary>
public sealed class DatabaseHealthCheck
{
    // TODO(v4 §18): implement Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    // (ping ApplicationDbContext). Register via health-check builder in the API host.
}
