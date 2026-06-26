namespace Notrelix.Domain.Documents.Blocks.Events;

public sealed record BlockCreatedDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    Guid BlockId,
    BlockType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CreatedBy);
