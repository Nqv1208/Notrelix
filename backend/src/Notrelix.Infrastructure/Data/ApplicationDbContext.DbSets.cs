// Account
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Invitations;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Accounts.Regions;
using Notrelix.Domain.Accounts.Scim;
using Notrelix.Application.Features.Accounts.Abstractions.Records;
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
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.WorkManagement.Relations;
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

public partial class ApplicationDbContext
{
    // Account
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountMember> AccountMembers => Set<AccountMember>();
    public DbSet<AccountInvitation> AccountInvitations => Set<AccountInvitation>();
    public DbSet<AccountDomain> AccountDomains => Set<AccountDomain>();
    public DbSet<AccountSettingRecord> AccountSettingsEntities => Set<AccountSettingRecord>();
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
    public DbSet<MfaRecoveryBatch> MfaRecoveryBatches => Set<MfaRecoveryBatch>();
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
    public DbSet<Notrelix.Infrastructure.Operations.Idempotency.IdempotencyRecord> IdempotencyRecords => Set<Notrelix.Infrastructure.Operations.Idempotency.IdempotencyRecord>();
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
    public DbSet<Domain.Analytics.Placements.WorkspaceWorkItemPlacementProjection> WorkspaceWorkItemPlacements => Set<Domain.Analytics.Placements.WorkspaceWorkItemPlacementProjection>();

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

}
