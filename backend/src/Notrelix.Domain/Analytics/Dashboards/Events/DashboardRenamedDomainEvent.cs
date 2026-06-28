namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardRenamedDomainEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
