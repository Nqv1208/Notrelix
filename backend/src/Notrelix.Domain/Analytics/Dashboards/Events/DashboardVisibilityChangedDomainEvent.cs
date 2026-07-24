namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-visibility-changed")]
public sealed record DashboardVisibilityChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    DashboardVisibility NewVisibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
