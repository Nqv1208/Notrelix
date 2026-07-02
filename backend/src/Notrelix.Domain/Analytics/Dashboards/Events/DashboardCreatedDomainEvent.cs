namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, CreatedBy);
