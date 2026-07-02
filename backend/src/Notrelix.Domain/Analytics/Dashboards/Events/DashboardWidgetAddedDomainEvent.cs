namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardWidgetAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
