namespace Notrelix.Domain.Documents.Versions.Events;

[EventName("documents.document-version-restored")]
public sealed record DocumentVersionRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    int VersionNumber,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
