namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardRestoredDomainEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
