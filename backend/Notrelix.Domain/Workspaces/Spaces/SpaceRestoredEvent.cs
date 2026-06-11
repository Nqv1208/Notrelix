using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Spaces;

public sealed record SpaceRestoredEvent(
    Guid WorkspaceId,
    Guid SpaceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
