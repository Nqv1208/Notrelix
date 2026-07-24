namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.field-option-updated")]
public sealed record FieldOptionUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
