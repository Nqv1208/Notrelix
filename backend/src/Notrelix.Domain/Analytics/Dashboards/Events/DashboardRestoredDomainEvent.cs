namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardRestoredDomainEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
