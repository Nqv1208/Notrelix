namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardDeletedDomainEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
