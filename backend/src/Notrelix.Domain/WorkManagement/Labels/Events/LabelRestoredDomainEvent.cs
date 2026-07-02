namespace Notrelix.Domain.WorkManagement.Labels.Events;

public sealed record LabelRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LabelId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RestoredBy);
