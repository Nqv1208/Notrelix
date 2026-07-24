namespace Notrelix.Domain.Documents.Blocks.Events;

[EventName("documents.block-restored")]
public sealed record BlockRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
