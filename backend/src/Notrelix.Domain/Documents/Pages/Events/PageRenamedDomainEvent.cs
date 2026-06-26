namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageRenamedDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
