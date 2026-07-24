namespace Notrelix.Domain.Documents.Blocks.Events;

[EventName("documents.block-properties-updated")]
public sealed record BlockPropertiesUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
