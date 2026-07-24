namespace Notrelix.Domain.Documents.Blocks.Events;

[EventName("documents.block-created")]
public sealed record BlockCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    Guid BlockId,
    BlockType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
