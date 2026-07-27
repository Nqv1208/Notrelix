namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.board-field-classification-updated")]
public sealed record BoardFieldClassificationUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    DataClassification Classification,
    bool IsSensitive,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
