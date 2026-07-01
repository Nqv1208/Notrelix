namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldClassificationUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    DataClassification Classification,
    bool IsSensitive,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
