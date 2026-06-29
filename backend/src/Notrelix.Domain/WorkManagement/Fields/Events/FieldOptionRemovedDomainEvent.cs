namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record FieldOptionRemovedDomainEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RemovedBy);
