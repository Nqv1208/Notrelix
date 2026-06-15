using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardBackgroundUpdatedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    string OldBackground,
    string NewBackground,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
