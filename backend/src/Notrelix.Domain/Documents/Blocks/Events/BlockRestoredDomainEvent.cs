namespace Notrelix.Domain.Documents.Blocks.Events;

public sealed record BlockRestoredDomainEvent(
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
