namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-deleted")]
public sealed record DashboardDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
