namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-archived")]
public sealed record DashboardArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
