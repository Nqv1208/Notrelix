using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Activity;

public sealed record ActivityLoggedEvent(
    Guid LogId,
    Guid WorkspaceId,
    ActivityType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
