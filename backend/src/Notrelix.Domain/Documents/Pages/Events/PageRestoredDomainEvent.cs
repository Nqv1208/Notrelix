namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageRestoredDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
