namespace Notrelix.Domain.Documents.Blocks.Events;

[EventName("documents.block-moved")]
public sealed record BlockMovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid? OldParentId,
    Guid? NewParentId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
