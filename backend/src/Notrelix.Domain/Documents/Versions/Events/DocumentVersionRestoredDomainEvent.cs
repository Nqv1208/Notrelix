namespace Notrelix.Domain.Documents.Versions.Events;

public sealed record DocumentVersionRestoredDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    int VersionNumber,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
