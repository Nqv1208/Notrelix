namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.field-option-added")]
public sealed record FieldOptionAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    string Name,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
