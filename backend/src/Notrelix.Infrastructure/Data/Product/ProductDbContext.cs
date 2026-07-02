using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Analytics.Abstractions;

using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Versions;

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

using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Domain.Collaboration.Presence;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.ReadStates;
using Notrelix.Domain.Collaboration.Watchers;

using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Automation.Templates;

using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Integrations.Sync;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.Integrations.Webhooks.Events;

using Notrelix.Domain.Billing.BillingEvents;
using Notrelix.Domain.Billing.Customers;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Payments;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Usage;

using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Snapshots;
using Notrelix.Infrastructure.Data.Analytics;

namespace Notrelix.Infrastructure.Data.Product;

/// <summary>
/// DbContext for product bounded-contexts: WorkManagement, Documents, Collaboration, Automation, Integrations, Billing, Analytics.
/// Schemas: work, docs, collab, automation, integration, billing, reporting
/// </summary>
public class ProductDbContext : BaseNotrelixDbContext,
    IWorkManagementDbContext, IDocumentDbContext, ICollaborationDbContext,
    IAutomationDbContext, IIntegrationDbContext, IBillingDbContext, IReportingDbContext
{
    public ProductDbContext(
        DbContextOptions<ProductDbContext> options,
        ICurrentWorkspace? currentWorkspace = null)
        : base(options, currentWorkspace) { }

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
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<FormQuestion> FormQuestions => Set<FormQuestion>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<WorkloadAllocation> WorkloadAllocations => Set<WorkloadAllocation>();
    public DbSet<BoardTemplate> BoardTemplates => Set<BoardTemplate>();
    public DbSet<ItemTemplate> ItemTemplates => Set<ItemTemplate>();

    // Document
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<ResourceLink> ResourceLinks => Set<ResourceLink>();
    public DbSet<PageTemplate> PageTemplates => Set<PageTemplate>();

    // Collaboration
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Mention> PageMentions => Set<Mention>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ResourceWatcher> ResourceWatchers => Set<ResourceWatcher>();
    public DbSet<PresenceSession> PresenceSessions => Set<PresenceSession>();
    public DbSet<ResourceReadState> ResourceReadStates => Set<ResourceReadState>();

    // Automation
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<AutomationExecution> AutomationExecutions => Set<AutomationExecution>();
    public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();
    public DbSet<AutomationTemplate> AutomationTemplates => Set<AutomationTemplate>();
    public DbSet<AiAgent> AiAgents => Set<AiAgent>();
    public DbSet<AiAgentRun> AiAgentRuns => Set<AiAgentRun>();

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

    // Analytics / Reporting
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();
    public DbSet<DashboardSource> DashboardSources => Set<DashboardSource>();
    public DbSet<ReportingSnapshot> ReportingSnapshots => Set<ReportingSnapshot>();
    public DbSet<WorkspaceUsageDaily> WorkspaceUsageDaily => Set<WorkspaceUsageDaily>();
    public DbSet<FeatureUsageDaily> FeatureUsageDaily => Set<FeatureUsageDaily>();

    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BaseNotrelixDbContext).Assembly,
            t => t.Namespace is not null && (
                t.Namespace.Contains(".Configurations.WorkManagement") ||
                t.Namespace.Contains(".Configurations.Documents") ||
                t.Namespace.Contains(".Configurations.Collaboration") ||
                t.Namespace.Contains(".Configurations.Automation") ||
                t.Namespace.Contains(".Configurations.Integrations") ||
                t.Namespace.Contains(".Configurations.Billing") ||
                t.Namespace.Contains(".Configurations.Analytics")));
    }
}
