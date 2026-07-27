namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-restored")]
public sealed record DashboardRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
