using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Accounts.Scim;
using Notrelix.Domain.Accounts.WorkspaceRoutes;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Snapshots;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.Scheduled;
using Notrelix.Domain.Automation.Templates;
using Notrelix.Domain.Billing.BillingEvents;
using Notrelix.Domain.Billing.Customers;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Payments;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Collaboration.Watchers;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Governance.Templates;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Integrations.Webhooks;
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

namespace Notrelix.Domain.Tests.Freeze;

internal enum AggregateDeletionPolicy
{
    NotSupported,
    RecoverableDelete,
    ArchiveOnly,
    BusinessTerminationOnly,
    AppendOnly,
    OwnedRemoval,
    BusinessTombstone
}

internal static class DeletionPolicyRegistry
{
    internal sealed record AggregateDeletionEntry(
        Type AggregateType,
        AggregateDeletionPolicy Policy);

    private static readonly AggregateDeletionEntry[] Entries =
    [
        // ── Accounts ────────────────────────────────────────────────────────
        new(typeof(Account), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(AccountMember), AggregateDeletionPolicy.OwnedRemoval),
        new(typeof(WorkspaceRoute), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(AccountDomain), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(AccountIdentityProvider), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(ScimDirectory), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(AccountInvitation), AggregateDeletionPolicy.BusinessTerminationOnly),

        // ── Identity ────────────────────────────────────────────────────────
        new(typeof(User), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(UserProfile), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(UserSession), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(UserSecuritySettings), AggregateDeletionPolicy.NotSupported),
        new(typeof(UserMfaMethod), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(ApiToken), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(EmailVerificationToken), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(PasswordResetToken), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(UserLoginAttempt), AggregateDeletionPolicy.AppendOnly),

        // ── Workspaces ──────────────────────────────────────────────────────
        new(typeof(Workspace), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(WorkspaceMember), AggregateDeletionPolicy.OwnedRemoval),
        new(typeof(WorkspaceInvitation), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(Space), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(Team), AggregateDeletionPolicy.RecoverableDelete),

        // ── WorkManagement ──────────────────────────────────────────────────
        new(typeof(Board), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(BoardField), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(BoardItem), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(BoardGroup), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(BoardView), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(BoardViewUserPreference), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(SavedFilter), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(BoardRelation), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(Checklist), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(ApprovalRequest), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(Form), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(Label), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(BoardTemplate), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(ItemTemplate), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(TimeTrackingEntry), AggregateDeletionPolicy.AppendOnly),

        // ── Documents ───────────────────────────────────────────────────────
        new(typeof(Page), AggregateDeletionPolicy.BusinessTombstone),
        new(typeof(Block), AggregateDeletionPolicy.BusinessTombstone),
        new(typeof(ResourceLink), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(DocumentVersion), AggregateDeletionPolicy.AppendOnly),
        new(typeof(PageTemplate), AggregateDeletionPolicy.ArchiveOnly),

        // ── Collaboration ───────────────────────────────────────────────────
        new(typeof(Comment), AggregateDeletionPolicy.BusinessTombstone),
        new(typeof(Reaction), AggregateDeletionPolicy.AppendOnly),
        new(typeof(ResourceWatcher), AggregateDeletionPolicy.AppendOnly),
        new(typeof(Attachment), AggregateDeletionPolicy.RecoverableDelete),

        // ── Governance ──────────────────────────────────────────────────────
        new(typeof(ResourcePermission), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(PermissionRule), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(CustomRole), AggregateDeletionPolicy.ArchiveOnly),
        new(typeof(ShareLink), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(PermissionTemplate), AggregateDeletionPolicy.ArchiveOnly),

        // ── Automation ──────────────────────────────────────────────────────
        new(typeof(AutomationRule), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(AutomationTemplate), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(ScheduledJob), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(AiAgent), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(AiAgentRun), AggregateDeletionPolicy.AppendOnly),
        new(typeof(AutomationExecution), AggregateDeletionPolicy.AppendOnly),

        // ── Integrations ────────────────────────────────────────────────────
        new(typeof(IntegrationConnection), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(WebhookSubscription), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(CalendarIntegration), AggregateDeletionPolicy.RecoverableDelete),
        new(typeof(WebhookDelivery), AggregateDeletionPolicy.AppendOnly),
        new(typeof(InboundWebhookEvent), AggregateDeletionPolicy.AppendOnly),

        // ── Billing ─────────────────────────────────────────────────────────
        new(typeof(Plan), AggregateDeletionPolicy.NotSupported),
        new(typeof(Entitlement), AggregateDeletionPolicy.NotSupported),
        new(typeof(BillingCustomer), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(Subscription), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(Invoice), AggregateDeletionPolicy.AppendOnly),
        new(typeof(PaymentMethod), AggregateDeletionPolicy.BusinessTerminationOnly),
        new(typeof(UsageMetric), AggregateDeletionPolicy.AppendOnly),
        new(typeof(WorkspaceFeatureUsage), AggregateDeletionPolicy.AppendOnly),
        new(typeof(BillingEvent), AggregateDeletionPolicy.AppendOnly),

        // ── Analytics ───────────────────────────────────────────────────────
        new(typeof(Dashboard), AggregateDeletionPolicy.ArchiveOnly),
        new(typeof(DashboardSource), AggregateDeletionPolicy.NotSupported),
        new(typeof(ReportingSnapshot), AggregateDeletionPolicy.AppendOnly),
    ];

    public static AggregateDeletionPolicy GetPolicy<T>() where T : class
        => GetPolicy(typeof(T));

    public static AggregateDeletionPolicy GetPolicy(Type type)
    {
        foreach (var entry in Entries)
            if (entry.AggregateType == type)
                return entry.Policy;
        throw new InvalidOperationException(
            $"Aggregate type {type.FullName} is not registered in {nameof(DeletionPolicyRegistry)}.");
    }

    public static IReadOnlyList<AggregateDeletionEntry> GetAll()
        => Entries.AsSpan().ToArray();
}
