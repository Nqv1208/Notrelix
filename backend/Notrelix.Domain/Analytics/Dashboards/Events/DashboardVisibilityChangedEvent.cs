using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardVisibilityChangedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    DashboardVisibility NewVisibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
