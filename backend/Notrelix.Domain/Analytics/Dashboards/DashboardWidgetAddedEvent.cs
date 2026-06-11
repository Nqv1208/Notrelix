using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Dashboards;

public sealed record DashboardWidgetAddedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
