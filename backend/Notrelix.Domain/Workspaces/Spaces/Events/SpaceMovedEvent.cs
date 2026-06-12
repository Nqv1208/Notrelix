using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceMovedEvent(
    Guid SpaceId,
    Guid OldWorkspaceId,
    Guid NewWorkspaceId,
    Guid MovedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
