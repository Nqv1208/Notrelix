namespace Notrelix.Domain.Documents.Blocks.Events;

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
