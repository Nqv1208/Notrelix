using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Dashboards;

public sealed record DashboardWidgetRemovedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
