namespace Notrelix.Domain.Documents.Blocks.Events;

public sealed record BlockSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
