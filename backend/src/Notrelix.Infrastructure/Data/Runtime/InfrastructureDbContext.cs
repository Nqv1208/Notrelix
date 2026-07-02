using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data.Abstractions;

// Events
using Notrelix.Infrastructure.Data.Events;

// Messaging
using Notrelix.Infrastructure.Data.Messaging;

// Audit
using Notrelix.Infrastructure.Data.Audit;

// Ops
using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Data.Runtime;

/// <summary>
/// DbContext for infrastructure/runtime contexts: Events, Messaging, Audit, Ops.
/// Schemas: events, messaging, audit, ops
/// </summary>
public class InfrastructureDbContext : BaseNotrelixDbContext,
    IMessagingDbContext, IAuditDbContext, IOpsDbContext
{
    public InfrastructureDbContext(
        DbContextOptions<InfrastructureDbContext> options,
        ICurrentWorkspace? currentWorkspace = null)
        : base(options, currentWorkspace) { }

    // Events
    public DbSet<DomainEventLog> DomainEventLogs => Set<DomainEventLog>();

    // Messaging
    public DbSet<MessagingOutboxMessage> MessagingOutboxMessages => Set<MessagingOutboxMessage>();
    public DbSet<OutboxDeliveryAttempt> OutboxDeliveryAttempts => Set<OutboxDeliveryAttempt>();
    public DbSet<MessagingProcessedEvent> MessagingProcessedEvents => Set<MessagingProcessedEvent>();

    // Audit
    public DbSet<AuditLog> EnterpriseAuditLogs => Set<AuditLog>();
    public DbSet<SecurityEvent> EnterpriseSecurityEvents => Set<SecurityEvent>();

    // Ops
    public DbSet<IdempotencyKeyRecord> IdempotencyKeys => Set<IdempotencyKeyRecord>();
    public DbSet<ImportJobRecord> ImportJobs => Set<ImportJobRecord>();
    public DbSet<ExportJobRecord> ExportJobs => Set<ExportJobRecord>();
    public DbSet<JobLockRecord> JobLocks => Set<JobLockRecord>();

    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BaseNotrelixDbContext).Assembly,
            t => t.Namespace is not null && (
                t.Namespace.Contains(".Configurations.Messaging") ||
                t.Namespace.Contains(".Configurations.Events") ||
                t.Namespace.Contains(".Configurations.Audit") ||
                t.Namespace.Contains(".Configurations.Ops")));
    }
}
