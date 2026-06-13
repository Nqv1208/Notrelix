using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardWidgetRemovedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
