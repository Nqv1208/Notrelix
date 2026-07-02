using System.Reflection;
using System.Text.Json;
using System.Linq.Expressions;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Infrastructure.Data.Abstractions;

// Account
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Invitations;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Accounts.Regions;
using Notrelix.Domain.Accounts.Scim;
using Notrelix.Domain.Accounts.Settings;
using Notrelix.Domain.Accounts.WorkspaceRoutes;

// Identity
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;

// Workspace
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Workspaces;

// Documents
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Versions;

// WorkManagement
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

// Collaboration
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Domain.Collaboration.Presence;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.ReadStates;
using Notrelix.Domain.Collaboration.Watchers;

// Governance
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Policies;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Governance.Templates;

// Automation
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Automation.Templates;

// Integrations
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Integrations.Sync;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.Integrations.Webhooks.Events;

// Billing
using Notrelix.Domain.Billing.Customers;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.BillingEvents;
using Notrelix.Domain.Billing.Payments;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Usage;

// Analytics
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Snapshots;

// Infrastructure projections & records
using Notrelix.Infrastructure.Data.Analytics;
using Notrelix.Infrastructure.Data.Audit;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Events;
using Notrelix.Infrastructure.Data.Governance.Projections;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Data.Ops.Entities;
using Notrelix.Infrastructure.Data.Projections.Activity;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Data;

public class ApplicationDbContext : DbContext,
    IApplicationDbContext,
    IWorkspaceDbContext,
    IWorkManagementDbContext,
    IIdentityDbContext,
    IAccountDbContext,
    IDocumentDbContext,
    ICollaborationDbContext,
    IAutomationDbContext,
    IGovernanceDbContext,
    IIntegrationDbContext,
    IBillingDbContext,
    IReportingDbContext,
    ISearchProjectionDbContext,
    IMessagingDbContext,
    IAuditDbContext,
    IOpsDbContext,
    IActivityProjectionDbContext,
    INotificationDbContext
{
    private readonly ICurrentWorkspace? _currentWorkspace;

    private static readonly FieldInfo CurrentWorkspaceField = typeof(ApplicationDbContext)
        .GetField("_currentWorkspace", BindingFlags.NonPublic | BindingFlags.Instance)!;

    protected ICurrentWorkspace? CurrentWorkspace => _currentWorkspace;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentWorkspace? currentWorkspace = null) : base(options)
    {
        _currentWorkspace = currentWorkspace;
    }

    // Account
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountMember> AccountMembers => Set<AccountMember>();
    public DbSet<AccountInvitation> AccountInvitations => Set<AccountInvitation>();
    public DbSet<AccountDomain> AccountDomains => Set<AccountDomain>();
    public DbSet<AccountSettings> AccountSettingsEntities => Set<AccountSettings>();
    public DbSet<AccountRegion> AccountRegions => Set<AccountRegion>();
    public DbSet<AccountIdentityProvider> AccountIdentityProviders => Set<AccountIdentityProvider>();
    public DbSet<ScimDirectory> ScimDirectories => Set<ScimDirectory>();
    public DbSet<ScimSyncRun> ScimSyncRuns => Set<ScimSyncRun>();
    public DbSet<WorkspaceRoute> WorkspaceRoutes => Set<WorkspaceRoute>();

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
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();

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
    public DbSet<WorkspacePolicy> WorkspacePolicies => Set<WorkspacePolicy>();
    public DbSet<PermissionTemplate> PermissionTemplates => Set<PermissionTemplate>();

    // Collaboration
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Mention> PageMentions => Set<Mention>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ResourceWatcher> ResourceWatchers => Set<ResourceWatcher>();
    public DbSet<PresenceSession> PresenceSessions => Set<PresenceSession>();

    // Billing
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanLimit> PlanLimits => Set<PlanLimit>();
    public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionItem> SubscriptionItems => Set<SubscriptionItem>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<BillingEvent> BillingEvents => Set<BillingEvent>();
    public DbSet<BillingCustomer> BillingCustomers => Set<BillingCustomer>();
    public DbSet<Entitlement> Entitlements => Set<Entitlement>();
    public DbSet<UsageMetric> UsageMetrics => Set<UsageMetric>();
    public DbSet<UsageMetricHistory> UsageMetricHistories => Set<UsageMetricHistory>();
    public DbSet<FeatureUsageLedger> FeatureUsageLedger => Set<FeatureUsageLedger>();

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

    // Analytics
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();
    public DbSet<DashboardSource> DashboardSources => Set<DashboardSource>();
    public DbSet<ReportingSnapshot> ReportingSnapshots => Set<ReportingSnapshot>();

    // Enterprise event store
    public DbSet<DomainEventLog> DomainEventLogs => Set<DomainEventLog>();

    // Enterprise messaging
    public DbSet<MessagingOutboxMessage> MessagingOutboxMessages => Set<MessagingOutboxMessage>();
    public DbSet<OutboxDeliveryAttempt> OutboxDeliveryAttempts => Set<OutboxDeliveryAttempt>();
    public DbSet<MessagingProcessedEvent> MessagingProcessedEvents => Set<MessagingProcessedEvent>();

    // Enterprise notifications
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
    public DbSet<EmailDeliveryAttempt> EmailDeliveryAttempts => Set<EmailDeliveryAttempt>();

    // Canonical notifications
    public DbSet<NotificationItemRecord> NotificationItems => Set<NotificationItemRecord>();
    public DbSet<NotificationRecipientRecord> NotificationRecipients => Set<NotificationRecipientRecord>();
    public DbSet<NotificationPreferenceRecord> CanonicalNotificationPreferences => Set<NotificationPreferenceRecord>();
    public DbSet<NotificationCounterRecord> NotificationCounters => Set<NotificationCounterRecord>();

    // Collaboration read states
    public DbSet<ResourceReadState> ResourceReadStates => Set<ResourceReadState>();

    // Enterprise audit
    public DbSet<AuditLog> EnterpriseAuditLogs => Set<AuditLog>();
    public DbSet<SecurityEvent> EnterpriseSecurityEvents => Set<SecurityEvent>();

    // Enterprise analytics
    public DbSet<WorkspaceUsageDaily> WorkspaceUsageDaily => Set<WorkspaceUsageDaily>();
    public DbSet<FeatureUsageDaily> FeatureUsageDaily => Set<FeatureUsageDaily>();

    // Enterprise authz
    public DbSet<AccessGrant> AccessGrants => Set<AccessGrant>();

    // Activity projection
    public DbSet<WorkspaceActivityLogRecord> WorkspaceActivityLogs => Set<WorkspaceActivityLogRecord>();
    public DbSet<ActivityReadStateRecord> ActivityReadStates => Set<ActivityReadStateRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");

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
                if (_currentWorkspace is null)
                {
                    // Null workspace → block all access (evaluated at model creation time).
                    // Cannot use runtime expression because InMemory provider doesn't short-circuit
                    // AndAlso, causing NullReferenceException when accessing _currentWorkspace properties.
                    var noAccess = Expression.Constant(false);
                    filterBody = filterBody is not null
                        ? Expression.AndAlso(filterBody, noAccess)
                        : noAccess;
                }
                else
                {
                    // Non-null workspace → runtime filter evaluated at QUERY TIME via EF Core's funcletizer.
                    // This ensures the filter adapts when the workspace context changes after model creation
                    // (e.g., switching from system context to a specific workspace).
                    //
                    // Expression: _currentWorkspace.IsSystemContext 
                    //     || (e.AccountId == _currentWorkspace.AccountId && e.WorkspaceId == _currentWorkspace.WorkspaceId)
                    // (_currentWorkspace is guaranteed non-null here, so property access is safe)

                    var contextField = Expression.Field(Expression.Constant(this), CurrentWorkspaceField);
                    var isSysProp = Expression.Property(contextField, nameof(ICurrentWorkspace.IsSystemContext));
                    var wsIdProp = Expression.Property(contextField, nameof(ICurrentWorkspace.WorkspaceId));
                    var acctIdProp = Expression.Property(contextField, nameof(ICurrentWorkspace.AccountId));

                    // Convert both sides to Guid? for comparison
                    var wsIdEquals = Expression.Equal(
                        Expression.Convert(Expression.PropertyOrField(param, "WorkspaceId"), typeof(Guid?)),
                        Expression.Convert(wsIdProp, typeof(Guid?)));

                    var acctIdEquals = Expression.Equal(
                        Expression.Convert(Expression.PropertyOrField(param, "AccountId"), typeof(Guid?)),
                        Expression.Convert(acctIdProp, typeof(Guid?)));

                    // AccountId AND WorkspaceId must both match
                    var tenantMatch = Expression.AndAlso(acctIdEquals, wsIdEquals);

                    var innerOr = Expression.OrElse(isSysProp, tenantMatch);

                    filterBody = filterBody is not null
                        ? Expression.AndAlso(filterBody, innerOr)
                        : innerOr;
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
                else if (property.ClrType == typeof(JsonDocument))
                    property.SetValueConverter(new Converters.JsonDocumentConverter());
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
