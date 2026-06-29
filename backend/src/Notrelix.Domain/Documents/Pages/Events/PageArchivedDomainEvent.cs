namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageArchivedDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ArchivedBy);
