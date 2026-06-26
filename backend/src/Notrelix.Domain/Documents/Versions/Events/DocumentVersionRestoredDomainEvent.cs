namespace Notrelix.Domain.Documents.Versions.Events;

public sealed record DocumentVersionRestoredDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    int VersionNumber,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
