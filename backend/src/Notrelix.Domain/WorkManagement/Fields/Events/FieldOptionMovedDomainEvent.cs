namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.field-option-moved")]
public sealed record FieldOptionMovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    Guid OptionId,
    FractionalIndex NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
