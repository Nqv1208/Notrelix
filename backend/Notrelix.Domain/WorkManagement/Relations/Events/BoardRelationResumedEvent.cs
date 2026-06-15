using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationResumedEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid ResumedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ResumedBy);
