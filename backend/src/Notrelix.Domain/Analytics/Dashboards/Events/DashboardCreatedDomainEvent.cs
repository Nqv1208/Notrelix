namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardCreatedDomainEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
