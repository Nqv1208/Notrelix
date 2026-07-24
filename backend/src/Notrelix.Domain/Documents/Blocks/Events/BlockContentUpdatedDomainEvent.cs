namespace Notrelix.Domain.Documents.Blocks.Events;

[EventName("documents.block-content-updated")]
public sealed record BlockContentUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
