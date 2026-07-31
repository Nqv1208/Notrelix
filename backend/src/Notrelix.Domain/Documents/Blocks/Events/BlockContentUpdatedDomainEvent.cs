namespace Notrelix.Domain.Documents.Blocks.Events;

[EventName("documents.block-content-updated", Version = 2)]
public sealed record BlockContentUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    BlockType BlockType,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
