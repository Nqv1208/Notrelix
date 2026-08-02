namespace Notrelix.Domain.SharedKernel;

/// <summary>
/// Deterministic mapping from legacy <see cref="ResourceType"/> enum values to canonical
/// <see cref="ResourceKind"/> strings. This mapping is immutable once released.
/// Used during migration to backfill and dual-read resource kind columns.
/// </summary>
public static class LegacyResourceTypeMappings
{
    private static readonly Dictionary<ResourceType, string> EnumToKind = new()
    {
        [ResourceType.Account] = "accounts.account",
        [ResourceType.Workspace] = "workspaces.workspace",
        [ResourceType.WorkspaceMember] = "workspaces.workspace-member",
        [ResourceType.Team] = "workspaces.team",
        [ResourceType.TeamMember] = "workspaces.team-member",
        [ResourceType.Space] = "workspaces.space",
        [ResourceType.Board] = "work-management.board",
        [ResourceType.BoardGroup] = "work-management.board-group",
        [ResourceType.BoardField] = "work-management.board-field",
        [ResourceType.BoardItem] = "work-management.board-item",
        [ResourceType.BoardView] = "work-management.board-view",
        [ResourceType.BoardRelation] = "work-management.board-relation",
        [ResourceType.Form] = "work-management.form",
        [ResourceType.FormSubmission] = "work-management.form-submission",
        [ResourceType.Checklist] = "work-management.checklist",
        [ResourceType.ChecklistItem] = "work-management.checklist-item",
        [ResourceType.ApprovalRequest] = "work-management.approval-request",
        [ResourceType.ApprovalStep] = "work-management.approval-step",
        [ResourceType.Page] = "documents.page",
        [ResourceType.Block] = "documents.block",
        [ResourceType.DocumentVersion] = "documents.document-version",
        [ResourceType.ResourceLink] = "documents.resource-link",
        [ResourceType.Dashboard] = "analytics.dashboard",
        [ResourceType.DashboardWidget] = "analytics.dashboard-widget",
        [ResourceType.Comment] = "collaboration.comment",
        [ResourceType.Attachment] = "collaboration.attachment",
        [ResourceType.Reaction] = "collaboration.reaction",
        [ResourceType.Mention] = "collaboration.mention",
        [ResourceType.Notification] = "notifications.notification",
        [ResourceType.ActivityLog] = "collaboration.activity-log",
        [ResourceType.ResourceWatcher] = "collaboration.resource-watcher",
        [ResourceType.AutomationRule] = "automation.rule",
        [ResourceType.AutomationExecution] = "automation.execution",
        [ResourceType.ScheduledJob] = "automation.scheduled-job",
        [ResourceType.AiAgent] = "automation.ai-agent",
        [ResourceType.AiAgentRun] = "automation.ai-agent-run",
        [ResourceType.IntegrationConnection] = "integrations.connection",
        [ResourceType.CalendarIntegration] = "integrations.calendar-integration",
        [ResourceType.CalendarEvent] = "integrations.calendar-event",
        [ResourceType.WebhookSubscription] = "integrations.webhook-subscription",
        [ResourceType.WebhookDelivery] = "integrations.webhook-delivery",
        [ResourceType.CustomRole] = "governance.custom-role",
        [ResourceType.PermissionRule] = "governance.permission-rule",
        [ResourceType.ResourcePermission] = "governance.resource-permission",
        [ResourceType.ShareLink] = "governance.share-link",
        [ResourceType.Subscription] = "billing.subscription",
        [ResourceType.Entitlement] = "billing.entitlement",
        [ResourceType.Invoice] = "billing.invoice",
        [ResourceType.PaymentMethod] = "billing.payment-method",
        [ResourceType.User] = "identity.user",
        [ResourceType.UserSession] = "identity.user-session",
        [ResourceType.ApiToken] = "identity.api-token",
        [ResourceType.AuditLog] = "governance.audit-log",
        [ResourceType.SecurityEvent] = "governance.security-event",
        [ResourceType.WorkspaceInvitation] = "workspaces.workspace-invitation",
        [ResourceType.ItemTemplate] = "work-management.item-template",
        [ResourceType.Label] = "work-management.label",
        [ResourceType.SavedFilter] = "work-management.saved-filter",
        [ResourceType.Plan] = "billing.plan",
        [ResourceType.UsageMetric] = "billing.usage-metric",
        [ResourceType.WorkspaceFeatureUsage] = "billing.workspace-feature-usage",
        [ResourceType.PageTemplate] = "documents.page-template",
        [ResourceType.DashboardSource] = "analytics.dashboard-source",
        [ResourceType.External] = "external.resource",
    };

    private static readonly Dictionary<string, ResourceType> KindToEnum =
        EnumToKind.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.Ordinal);

    /// <summary>
    /// Maps a legacy enum value to its canonical ResourceKind string.
    /// Throws for unknown enum values.
    /// </summary>
    public static ResourceKind ToResourceKind(ResourceType resourceType)
    {
        if (!EnumToKind.TryGetValue(resourceType, out var kind))
            throw new ArgumentOutOfRangeException(
                nameof(resourceType), resourceType,
                "No canonical ResourceKind mapping exists for this ResourceType value.");

        return ResourceKind.Create(kind);
    }

    /// <summary>
    /// Attempts to resolve a canonical kind string back to the legacy enum.
    /// Returns false for kinds that have no legacy equivalent (new resources).
    /// </summary>
    public static bool TryToLegacyEnum(string kind, out ResourceType resourceType)
    {
        return KindToEnum.TryGetValue(kind, out resourceType);
    }

    /// <summary>
    /// All defined mappings. Count must equal the number of ResourceType enum values.
    /// </summary>
    public static IReadOnlyDictionary<ResourceType, string> All => EnumToKind;
}
