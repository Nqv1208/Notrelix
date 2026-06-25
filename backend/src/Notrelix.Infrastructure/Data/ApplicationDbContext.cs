using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Snapshots;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Automation.Templates;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.BillingEvents;
using Notrelix.Domain.Billing.Payments;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Collaboration.Activity;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Collaboration.Presence;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Common;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.Governance.Audit;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Policies;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Governance.Security.Events;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Governance.Templates;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Integrations.Sync;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.Integrations.Webhooks.Events;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Formulas;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.WorkManagement.Relations;
using Notrelix.Domain.WorkManagement.Rollups;
using Notrelix.Domain.WorkManagement.Templates;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.WorkManagement.Workload;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Outbox;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Data.Projections.Collab;
using Notrelix.Infrastructure.Data.Ops.Entities;
using Notrelix.Infrastructure.Data.Governance.Projections;
using System.Linq.Expressions;

namespace Notrelix.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentWorkspace? _currentWorkspace;

    private static readonly FieldInfo CurrentWorkspaceField = typeof(ApplicationDbContext)
        .GetField("_currentWorkspace", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentWorkspace? currentWorkspace = null) : base(options)
    {
        _currentWorkspace = currentWorkspace;
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();
    public DbSet<UserSecuritySettings> UserSecuritySettings => Set<UserSecuritySettings>();
    public DbSet<UserMfaMethod> UserMfaMethods => Set<UserMfaMethod>();
    public DbSet<UserLoginAttempt> UserLoginAttempts => Set<UserLoginAttempt>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<SsoProvider> SsoProviders => Set<SsoProvider>();
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();
    public DbSet<ScimDirectorySync> ScimDirectorySyncs => Set<ScimDirectorySync>();

    // Workspace
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    // Document
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<ResourceLink> ResourceLinks => Set<ResourceLink>();
    public DbSet<PageTemplate> PageTemplates => Set<PageTemplate>();

    // Board / WorkManagement
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardGroup> BoardGroups => Set<BoardGroup>();
    public DbSet<BoardField> BoardFields => Set<BoardField>();
    public DbSet<FieldOption> FieldOptions => Set<FieldOption>();
    public DbSet<BoardView> BoardViews => Set<BoardView>();
    public DbSet<BoardViewPin> BoardViewPins => Set<BoardViewPin>();
    public DbSet<BoardViewUserPreference> BoardViewUserPreferences => Set<BoardViewUserPreference>();
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();
    public DbSet<BoardItem> BoardItems => Set<BoardItem>();
    public DbSet<BoardItemValue> BoardItemValues => Set<BoardItemValue>();
    public DbSet<BoardItemMember> BoardItemMembers => Set<BoardItemMember>();
    public DbSet<BoardItemLabel> BoardItemLabels => Set<BoardItemLabel>();
    public DbSet<BoardItemLink> BoardItemLinks => Set<BoardItemLink>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<BoardSubscriber> BoardSubscribers => Set<BoardSubscriber>();
    public DbSet<BoardRelation> BoardRelations => Set<BoardRelation>();
    public DbSet<BoardItemConnection> BoardItemConnections => Set<BoardItemConnection>();
    public DbSet<MirrorValueSnapshot> MirrorValueSnapshots => Set<MirrorValueSnapshot>();
    public DbSet<ItemDependency> ItemDependencies => Set<ItemDependency>();
    public DbSet<TimeTrackingEntry> TimeTrackingEntries => Set<TimeTrackingEntry>();
    public DbSet<RelationFieldConfig> RelationFieldConfigs => Set<RelationFieldConfig>();
    public DbSet<FormulaDependency> FormulaDependencies => Set<FormulaDependency>();
    public DbSet<RollupSnapshot> RollupSnapshots => Set<RollupSnapshot>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<FormQuestion> FormQuestions => Set<FormQuestion>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<WorkloadAllocation> WorkloadAllocations => Set<WorkloadAllocation>();
    public DbSet<BoardTemplate> BoardTemplates => Set<BoardTemplate>();
    public DbSet<ItemTemplate> ItemTemplates => Set<ItemTemplate>();

    // Calendar / Integration
    public DbSet<CalendarIntegration> CalendarIntegrations => Set<CalendarIntegration>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<CalendarEventLink> CalendarEventLinks => Set<CalendarEventLink>();
    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();
    public DbSet<IntegrationScope> IntegrationScopes => Set<IntegrationScope>();
    public DbSet<IntegrationSecretVersion> IntegrationSecretVersions => Set<IntegrationSecretVersion>();
    public DbSet<IntegrationSyncCursor> IntegrationSyncCursors => Set<IntegrationSyncCursor>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<InboundWebhookEvent> InboundWebhookEvents => Set<InboundWebhookEvent>();

    // Automation
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<AutomationExecution> AutomationExecutions => Set<AutomationExecution>();
    public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();
    public DbSet<AutomationTemplate> AutomationTemplates => Set<AutomationTemplate>();
    public DbSet<AiAgent> AiAgents => Set<AiAgent>();
    public DbSet<AiAgentRun> AiAgentRuns => Set<AiAgentRun>();

    // Governance
    public DbSet<ResourcePermission> ResourcePermissions => Set<ResourcePermission>();
    public DbSet<FieldPermission> FieldPermissions => Set<FieldPermission>();
    public DbSet<PermissionRule> PermissionRules => Set<PermissionRule>();
    public DbSet<CustomRole> CustomRoles => Set<CustomRole>();
    public DbSet<CustomRolePermission> CustomRolePermissions => Set<CustomRolePermission>();
    public DbSet<MemberRoleAssignment> MemberRoleAssignments => Set<MemberRoleAssignment>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditRetentionPolicy> AuditRetentionPolicies => Set<AuditRetentionPolicy>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<WorkspacePolicy> WorkspacePolicies => Set<WorkspacePolicy>();
    public DbSet<PermissionTemplate> PermissionTemplates => Set<PermissionTemplate>();

    // Collaboration
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Mention> PageMentions => Set<Mention>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<ResourceWatcher> ResourceWatchers => Set<ResourceWatcher>();
    public DbSet<PresenceSession> PresenceSessions => Set<PresenceSession>();

    // Billing
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanLimit> PlanLimits => Set<PlanLimit>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<BillingEvent> BillingEvents => Set<BillingEvent>();
    public DbSet<Entitlement> Entitlements => Set<Entitlement>();
    public DbSet<UsageMetric> UsageMetrics => Set<UsageMetric>();
    public DbSet<UsageMetricHistory> UsageMetricHistories => Set<UsageMetricHistory>();
    public DbSet<WorkspaceFeatureUsage> WorkspaceFeatureUsages => Set<WorkspaceFeatureUsage>();
    public DbSet<FeatureUsageLedger> FeatureUsageLedger => Set<FeatureUsageLedger>();

    // Infrastructure
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    // Search projections
    public DbSet<SearchDocumentRecord> SearchDocuments => Set<SearchDocumentRecord>();
    public DbSet<SearchIndexJobRecord> SearchIndexJobs => Set<SearchIndexJobRecord>();

    // Ops infrastructure records
    public DbSet<IdempotencyKeyRecord> IdempotencyKeys => Set<IdempotencyKeyRecord>();
    public DbSet<ImportJobRecord> ImportJobs => Set<ImportJobRecord>();
    public DbSet<ExportJobRecord> ExportJobs => Set<ExportJobRecord>();
    public DbSet<JobLockRecord> JobLocks => Set<JobLockRecord>();

    // Governance permission cache
    public DbSet<ResourcePermissionInheritanceCacheEntry> PermissionCache => Set<ResourcePermissionInheritanceCacheEntry>();

    // Collab unread counters
    public DbSet<UnreadCounterRecord> UnreadCounters => Set<UnreadCounterRecord>();

    // Analytics
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();
    public DbSet<DashboardSource> DashboardSources => Set<DashboardSource>();
    public DbSet<ReportingSnapshot> ReportingSnapshots => Set<ReportingSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(AggregateRoot.Version))
                    .HasColumnName("version")
                    .IsConcurrencyToken()
                    .HasDefaultValue(1L);
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isSoftDeletable = typeof(SoftDeletableEntity).IsAssignableFrom(entityType.ClrType);
            var isWorkspaceScoped = typeof(IWorkspaceScoped).IsAssignableFrom(entityType.ClrType);

            if (!isSoftDeletable && !isWorkspaceScoped)
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            Expression? filterBody = null;

            if (isSoftDeletable)
            {
                filterBody = Expression.Equal(
                    Expression.PropertyOrField(param, "DeletedAt"),
                    Expression.Constant(null, typeof(DateTimeOffset?)));
            }

            if (isWorkspaceScoped)
            {
                if (_currentWorkspace?.IsSystemContext == true)
                {
                }
                else if (_currentWorkspace?.HasWorkspace == true)
                {
                    var wsFilter = Expression.Equal(
                        Expression.PropertyOrField(param, "WorkspaceId"),
                        Expression.Convert(
                            Expression.Property(
                                Expression.Field(Expression.Constant(this), CurrentWorkspaceField),
                                "WorkspaceId"),
                            typeof(Guid)));

                    filterBody = filterBody is not null
                        ? Expression.AndAlso(filterBody, wsFilter)
                        : wsFilter;
                }
                else
                {
                    var noAccess = Expression.Constant(false);
                    filterBody = filterBody is not null
                        ? Expression.AndAlso(filterBody, noAccess)
                        : noAccess;
                }
            }

            if (filterBody is not null)
            {
                var lambda = Expression.Lambda(filterBody, param);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(Entity.Id))
                    .ValueGeneratedNever();
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(JsonValue))
                {
                    property.SetValueConverter(new Converters.JsonValueConverter());
                    property.SetColumnType("jsonb");
                }
                else if (property.ClrType == typeof(FractionalIndex))
                    property.SetValueConverter(new Converters.FractionalIndexConverter());
                else if (property.ClrType == typeof(SecretRef))
                    property.SetValueConverter(new Converters.SecretRefConverter());
                else if (property.ClrType == typeof(TokenHash))
                    property.SetValueConverter(new Converters.TokenHashConverter());
                else if (property.ClrType == typeof(DocumentSnapshot))
                    property.SetValueConverter(new Converters.DocumentSnapshotConverter());
                else if (property.ClrType == typeof(UsageMetricKey))
                    property.SetValueConverter(new Converters.UsageMetricKeyConverter());
                else if (property.ClrType == typeof(GroupRule))
                    property.SetValueConverter(new Converters.GroupRuleConverter());
                else if (property.ClrType == typeof(SyncCursorValue))
                    property.SetValueConverter(new Converters.SyncCursorValueConverter());
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    var converterType = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    property.SetValueConverter(converter);
                    property.SetMaxLength(50);
                }
            }
        }
    }
}
