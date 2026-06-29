namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardWidgetRemovedDomainEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
