using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Dashboards;

public sealed record DashboardRenamedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
