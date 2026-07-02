namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record FieldOptionRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RemovedBy);
