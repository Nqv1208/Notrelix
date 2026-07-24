namespace Notrelix.Domain.Documents.Versions.Events;

[EventName("documents.document-version-created")]
public sealed record DocumentVersionCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    int VersionNumber,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
