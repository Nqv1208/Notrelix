using Notrelix.Application.Common.SystemOperations;

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
/// no tenant (anonymous and pre-authentication failures are common), so the insert runs under an
/// explicit, logged system (worker) context with the RLS worker scope of <c>audit.security_events</c>;
/// RLS is not disabled and only that table is written.
/// </summary>
public sealed class SecurityAuditSink : ISecurityAuditSink
{
    private static readonly SystemOperationReason Reason =
        new("Security", "Persist a security audit event that has no request tenant scope");

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
            var tenant = services.GetRequiredService<ICurrentTenantContext>();
            var rls = services.GetRequiredService<IRlsSessionContext>();

            using var system = new SystemContextScope(tenant, _logger, "security-audit.write", Reason);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await rls.ApplyAsync(cancellationToken);

            context.EnterpriseSecurityEvents.Add(securityEvent);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // An audit failure must never fail the request, but it must not be silent either.
            _logger.LogError(ex, "Security audit event {EventType} could not be persisted", securityEvent.EventType);
        }
    }
}
