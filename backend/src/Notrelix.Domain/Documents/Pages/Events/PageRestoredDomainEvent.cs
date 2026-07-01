namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
