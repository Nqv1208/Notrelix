namespace Notrelix.Domain.Documents.Versions.Events;

public sealed record DocumentVersionCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    int VersionNumber,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
