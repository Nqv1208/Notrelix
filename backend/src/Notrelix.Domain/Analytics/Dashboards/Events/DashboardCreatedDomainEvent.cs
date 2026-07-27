namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-created")]
public sealed record DashboardCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
