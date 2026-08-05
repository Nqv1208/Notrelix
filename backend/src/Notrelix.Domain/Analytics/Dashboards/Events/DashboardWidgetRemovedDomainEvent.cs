namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-widget-removed")]
public sealed record DashboardWidgetRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
