using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Automation.Templates;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.BillingEvents;
using Notrelix.Domain.Billing.Payments;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Collaboration.Presence;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.Governance.Templates;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Integrations.Sync;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Formulas;
using Notrelix.Domain.WorkManagement.Relations;
using Notrelix.Domain.WorkManagement.Rollups;
using Notrelix.Domain.WorkManagement.Templates;
using Notrelix.Domain.WorkManagement.Workload;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;

namespace Notrelix.Application.Common.Abstractions;

public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }
    // Identity
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<UserSession> Sessions { get; }
    DbSet<OAuthAccount> OAuthAccounts { get; }
    DbSet<UserSecuritySettings> UserSecuritySettings { get; }
    DbSet<UserMfaMethod> UserMfaMethods { get; }
    DbSet<UserLoginAttempt> UserLoginAttempts { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<SsoProvider> SsoProviders { get; }
    DbSet<ApiToken> ApiTokens { get; }
    DbSet<ScimDirectorySync> ScimDirectorySyncs { get; }

    // Workspace
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<WorkspaceInvitation> WorkspaceInvitations { get; }
    DbSet<Space> Spaces { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }

    // Document
    DbSet<Page> Pages { get; }
    DbSet<Block> Blocks { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<ResourceLink> ResourceLinks { get; }
    DbSet<PageTemplate> PageTemplates { get; }

    // Board / WorkManagement
    DbSet<Board> Boards { get; }
    DbSet<BoardGroup> BoardGroups { get; }
    DbSet<BoardField> BoardFields { get; }
    DbSet<FieldOption> FieldOptions { get; }
    DbSet<BoardView> BoardViews { get; }
    DbSet<BoardViewPin> BoardViewPins { get; }
    DbSet<BoardViewUserPreference> BoardViewUserPreferences { get; }
    DbSet<SavedFilter> SavedFilters { get; }
    DbSet<BoardItem> BoardItems { get; }
    DbSet<BoardItemValue> BoardItemValues { get; }
    DbSet<BoardItemMember> BoardItemMembers { get; }
    DbSet<BoardItemLabel> BoardItemLabels { get; }
    DbSet<BoardItemLink> BoardItemLinks { get; }
    DbSet<Label> Labels { get; }
    DbSet<Checklist> Checklists { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }
    DbSet<BoardMember> BoardMembers { get; }
    DbSet<BoardSubscriber> BoardSubscribers { get; }
    DbSet<BoardRelation> BoardRelations { get; }
    DbSet<BoardItemConnection> BoardItemConnections { get; }
    DbSet<MirrorValueSnapshot> MirrorValueSnapshots { get; }
    DbSet<ItemDependency> ItemDependencies { get; }
    DbSet<TimeTrackingEntry> TimeTrackingEntries { get; }
    DbSet<RelationFieldConfig> RelationFieldConfigs { get; }
    DbSet<FormulaDependency> FormulaDependencies { get; }
    DbSet<RollupSnapshot> RollupSnapshots { get; }
    DbSet<Form> Forms { get; }
    DbSet<FormQuestion> FormQuestions { get; }
    DbSet<FormSubmission> FormSubmissions { get; }
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<ApprovalStep> ApprovalSteps { get; }
    DbSet<WorkloadAllocation> WorkloadAllocations { get; }
    DbSet<BoardTemplate> BoardTemplates { get; }
    DbSet<ItemTemplate> ItemTemplates { get; }

    // Calendar / Integration
    DbSet<CalendarIntegration> CalendarIntegrations { get; }
    DbSet<CalendarEvent> CalendarEvents { get; }
    DbSet<CalendarEventLink> CalendarEventLinks { get; }
    DbSet<IntegrationConnection> IntegrationConnections { get; }
    DbSet<IntegrationScope> IntegrationScopes { get; }
    DbSet<IntegrationSecretVersion> IntegrationSecretVersions { get; }
    DbSet<IntegrationSyncCursor> IntegrationSyncCursors { get; }
    DbSet<WebhookSubscription> WebhookSubscriptions { get; }
    DbSet<WebhookDelivery> WebhookDeliveries { get; }
    DbSet<InboundWebhookEvent> InboundWebhookEvents { get; }

    // Automation
    DbSet<AutomationRule> AutomationRules { get; }
    DbSet<AutomationExecution> AutomationExecutions { get; }
    DbSet<ScheduledJob> ScheduledJobs { get; }
    DbSet<AutomationTemplate> AutomationTemplates { get; }
    DbSet<AiAgent> AiAgents { get; }
    DbSet<AiAgentRun> AiAgentRuns { get; }

    // Governance
    DbSet<ResourcePermission> ResourcePermissions { get; }
    DbSet<FieldPermission> FieldPermissions { get; }
    DbSet<PermissionRule> PermissionRules { get; }
    DbSet<CustomRole> CustomRoles { get; }
    DbSet<CustomRolePermission> CustomRolePermissions { get; }
    DbSet<MemberRoleAssignment> MemberRoleAssignments { get; }
    DbSet<ShareLink> ShareLinks { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AuditRetentionPolicy> AuditRetentionPolicies { get; }
    DbSet<SecurityEvent> SecurityEvents { get; }
    DbSet<WorkspacePolicy> WorkspacePolicies { get; }
    DbSet<PermissionTemplate> PermissionTemplates { get; }

    // Collaboration
    DbSet<Comment> Comments { get; }
    DbSet<Mention> PageMentions { get; }
    DbSet<Reaction> Reactions { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<ResourceWatcher> ResourceWatchers { get; }
    DbSet<PresenceSession> PresenceSessions { get; }

    // Billing
    DbSet<Plan> Plans { get; }
    DbSet<PlanLimit> PlanLimits { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<BillingEvent> BillingEvents { get; }
    DbSet<Entitlement> Entitlements { get; }
    DbSet<UsageMetric> UsageMetrics { get; }
    DbSet<UsageMetricHistory> UsageMetricHistories { get; }
    DbSet<WorkspaceFeatureUsage> WorkspaceFeatureUsages { get; }
    DbSet<FeatureUsageLedger> FeatureUsageLedger { get; }

    // Analytics
    DbSet<Dashboard> Dashboards { get; }
    DbSet<DashboardWidget> DashboardWidgets { get; }
    DbSet<DashboardSource> DashboardSources { get; }
    DbSet<ReportingSnapshot> ReportingSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
