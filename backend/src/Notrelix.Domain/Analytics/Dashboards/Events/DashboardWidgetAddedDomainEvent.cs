namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardWidgetAddedDomainEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
