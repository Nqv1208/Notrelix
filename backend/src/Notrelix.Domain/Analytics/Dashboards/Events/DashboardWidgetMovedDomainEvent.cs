using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardWidgetMovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid DashboardId,
    Guid WidgetId,
    WidgetPosition NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
