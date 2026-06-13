using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Activity.Events;

public sealed record ActivityLoggedEvent(
    Guid LogId,
    Guid WorkspaceId,
    ActivityType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
