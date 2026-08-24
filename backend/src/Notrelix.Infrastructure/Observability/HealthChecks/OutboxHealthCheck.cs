using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Observability.HealthChecks;

public sealed class OutboxHealthCheckOptions
{
    public int DegradedPendingAgeMinutes { get; init; } = 5;
    public int UnhealthyPendingAgeMinutes { get; init; } = 15;
    public int UnhealthyDeadLetterCount { get; init; } = 1;
}

public sealed class OutboxHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly OutboxHealthCheckOptions _options;

    public OutboxHealthCheck(ApplicationDbContext context, IOptions<OutboxHealthCheckOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;

            var oldestPending = await _context.Set<MessagingOutboxMessage>()
                .Where(m => m.Status == "Pending")
                .MinAsync(m => (DateTimeOffset?)m.CreatedAt, cancellationToken);

            var failedCount = await _context.Set<MessagingOutboxMessage>()
                .CountAsync(m => m.Status == "Failed", cancellationToken);

            var deadLetterCount = await _context.Set<MessagingOutboxMessage>()
                .CountAsync(
                    m => m.Status == "Failed" && m.RetryCount >= m.MaxRetries,
                    cancellationToken);

            if (deadLetterCount >= _options.UnhealthyDeadLetterCount)
                return HealthCheckResult.Unhealthy(
                    $"Dead-letter count: {deadLetterCount}");

            if (oldestPending.HasValue)
            {
                var age = now - oldestPending.Value;
                if (age.TotalMinutes >= _options.UnhealthyPendingAgeMinutes)
                    return HealthCheckResult.Unhealthy(
                        $"Oldest pending message age: {age.TotalMinutes:F1} min");
                if (age.TotalMinutes >= _options.DegradedPendingAgeMinutes)
                    return HealthCheckResult.Degraded(
                        $"Oldest pending message age: {age.TotalMinutes:F1} min");
            }

            var description = $"Pending healthy. Oldest age: {oldestPending?.ToString() ?? "none"}";
            if (failedCount > 0)
                description += $". Failed count: {failedCount}";

            return HealthCheckResult.Healthy(description);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Outbox health check failed", ex);
        }
    }
}
