using Notrelix.Infrastructure.Data.Rls;

namespace Notrelix.Infrastructure.Data.Audit;

/// <summary>
/// Durable write path for operator security audit events raised outside a use case (for example by
/// middleware after the response was produced, when the request's data session has already
/// committed). Not a use-case port: business state is never written through it.
/// </summary>
public interface ISecurityAuditSink
{
    /// <summary>
    /// Persists the event in its own short transaction and returns when it is committed.
    /// Implementations must not throw into the caller's request pipeline.
    /// </summary>
    Task WriteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Writes audit events in an independent DI scope and transaction. The request's own data session
/// is never reused: it is closed by the time the middleware sees the status code, and sharing it
/// would either lose the event (no save) or reopen the request's transaction model. The event has
/// no tenant (anonymous and pre-authentication failures are common), which no tenant-scoped policy
/// admits, so on PostgreSQL the row is always written through the
/// owner-defined insert-only <c>ops.record_security_event</c> function: no system scope, no read, and the
/// actor and tenant of the row are derived from the scope the sink enters (the user of the request, or
/// none), never taken from the caller. Only a provider without that function (the in-memory test provider)
/// inserts the row directly.
/// </summary>
/// <remarks>
/// A failed write never fails the request, and is never silent: it is logged as an error and counted.
/// </remarks>
public sealed class SecurityAuditSink : ISecurityAuditSink
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityAuditSink> _logger;

    public SecurityAuditSink(IServiceScopeFactory scopeFactory, ILogger<SecurityAuditSink> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task WriteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            var rls = services.GetRequiredService<IRlsSessionContext>();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            if (context.Database.IsNpgsql())
            {
                if (securityEvent.UserId is { } userId && userId != Guid.Empty)
                {
                    await rls.ApplyUserScopeAsync(userId, cancellationToken);
                }

                var metadata = securityEvent.MetadataJson.RootElement.GetRawText();
                var occurredAt = securityEvent.OccurredAt.ToUniversalTime();
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    SELECT ops.record_security_event(
                        {securityEvent.EventType}, {securityEvent.Severity}, {securityEvent.Outcome},
                        {securityEvent.RiskScore}, {securityEvent.IpAddress}, {securityEvent.UserAgent},
                        {securityEvent.DeviceId}, {securityEvent.SessionId}, {securityEvent.ResourceKind},
                        {securityEvent.ResourceId}, {securityEvent.CorrelationId}, {metadata}::jsonb, {occurredAt})
                    """, cancellationToken);
            }
            else
            {
                context.EnterpriseSecurityEvents.Add(securityEvent);
                await context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            RlsScopeMetrics.RecordSecurityAudit(written: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // An audit failure must never fail the request, but it must not be silent either.
            RlsScopeMetrics.RecordSecurityAudit(written: false);
            _logger.LogError(ex, "Security audit event {EventType} could not be persisted", securityEvent.EventType);
        }
    }
}
