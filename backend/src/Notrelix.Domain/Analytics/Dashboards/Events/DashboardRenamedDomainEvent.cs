namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-renamed")]
public sealed record DashboardRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
