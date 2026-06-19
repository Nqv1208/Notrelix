using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardItemIdentityGeneratedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    long SequenceNumber,
    string ItemKey,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
