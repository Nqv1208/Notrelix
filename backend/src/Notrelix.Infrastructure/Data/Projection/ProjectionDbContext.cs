using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data.Abstractions;

// Search projections
using Notrelix.Infrastructure.Data.Projections.Search;

// Notifications
using Notrelix.Infrastructure.Data.Notifications;

// Activity projections
using Notrelix.Infrastructure.Data.Projections.Activity;

// Messaging (shared for search/notification infra)
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Data.Projection;

/// <summary>
/// DbContext for projection/query-model contexts: Search, Notifications, Activity.
/// Schemas: search, notifications, activity
/// </summary>
public class ProjectionDbContext : BaseNotrelixDbContext,
    ISearchProjectionDbContext, INotificationDbContext, IActivityProjectionDbContext
{
    public ProjectionDbContext(
        DbContextOptions<ProjectionDbContext> options,
        ICurrentWorkspace? currentWorkspace = null)
        : base(options, currentWorkspace) { }

    // Search projections
    public DbSet<SearchDocumentRecord> SearchDocuments => Set<SearchDocumentRecord>();
    public DbSet<SearchIndexJobRecord> SearchIndexJobs => Set<SearchIndexJobRecord>();

    // Notifications
    public DbSet<NotificationItemRecord> NotificationItems => Set<NotificationItemRecord>();
    public DbSet<NotificationRecipientRecord> NotificationRecipients => Set<NotificationRecipientRecord>();
    public DbSet<NotificationPreferenceRecord> CanonicalNotificationPreferences => Set<NotificationPreferenceRecord>();
    public DbSet<NotificationCounterRecord> NotificationCounters => Set<NotificationCounterRecord>();
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
    public DbSet<EmailDeliveryAttempt> EmailDeliveryAttempts => Set<EmailDeliveryAttempt>();

    // Activity projections
    public DbSet<WorkspaceActivityLogRecord> WorkspaceActivityLogs => Set<WorkspaceActivityLogRecord>();
    public DbSet<ActivityReadStateRecord> ActivityReadStates => Set<ActivityReadStateRecord>();

    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BaseNotrelixDbContext).Assembly,
            t => t.Namespace is not null && (
                t.Namespace.Contains(".Configurations.Search") ||
                t.Namespace.Contains(".Configurations.Notifications") ||
                t.Namespace.Contains(".Configurations.Activity")));
    }
}
