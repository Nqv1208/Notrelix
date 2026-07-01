namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardVisibilityChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    DashboardVisibility NewVisibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
