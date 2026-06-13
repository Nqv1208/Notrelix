using System;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Dashboards.Events;

public sealed record DashboardSourceAddedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid SourceId,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);

public sealed record DashboardSourceUpdatedEvent(
    Guid WorkspaceId,
    Guid DashboardId,
    Guid SourceId,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorUserId);
