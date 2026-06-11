using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Watchers;

public sealed record ResourceUnwatchedEvent(
    Guid WorkspaceId,
    Guid WatcherId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
