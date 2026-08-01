namespace Notrelix.Domain.Documents.Blocks.Events;

[EventName("documents.block-deleted")]
public sealed record BlockDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
