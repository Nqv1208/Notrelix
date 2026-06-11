using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Watchers;

public sealed record ResourceWatchedEvent(
    Guid WorkspaceId,
    Guid WatcherId,
    ResourceRef Target,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
