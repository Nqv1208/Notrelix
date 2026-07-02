namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardSourceUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid SourceId,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, ActorUserId);
