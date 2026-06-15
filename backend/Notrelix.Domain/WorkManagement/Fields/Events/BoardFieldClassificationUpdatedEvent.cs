using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldClassificationUpdatedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    DataClassification Classification,
    bool IsSensitive,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
