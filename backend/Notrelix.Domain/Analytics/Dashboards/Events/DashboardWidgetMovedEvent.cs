using Notrelix.Domain.Common;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardWidgetMovedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    WidgetPosition NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
