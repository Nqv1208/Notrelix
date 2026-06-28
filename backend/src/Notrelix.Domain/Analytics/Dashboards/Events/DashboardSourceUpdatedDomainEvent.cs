namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardSourceUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid SourceId,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActorUserId);
