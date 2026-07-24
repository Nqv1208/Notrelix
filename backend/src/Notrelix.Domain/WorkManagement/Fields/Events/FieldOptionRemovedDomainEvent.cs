namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.field-option-removed")]
public sealed record FieldOptionRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
