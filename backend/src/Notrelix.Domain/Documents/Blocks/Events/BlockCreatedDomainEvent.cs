namespace Notrelix.Domain.Documents.Blocks.Events;

public sealed record BlockCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    Guid BlockId,
    BlockType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
