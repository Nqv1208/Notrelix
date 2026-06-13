using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardCreatedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CreatedBy);
