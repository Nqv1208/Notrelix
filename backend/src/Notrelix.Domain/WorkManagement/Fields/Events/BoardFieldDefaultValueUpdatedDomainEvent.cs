namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.field-default-value-updated")]
public sealed record BoardFieldDefaultValueUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    FieldValue? DefaultValue,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
