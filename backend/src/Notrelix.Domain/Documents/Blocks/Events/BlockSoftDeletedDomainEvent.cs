namespace Notrelix.Domain.Documents.Blocks.Events;

[EventName("documents.block-soft-deleted")]
public sealed record BlockSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
