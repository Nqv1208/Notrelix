namespace Notrelix.Domain.Documents.Pages.Events;

[EventName("documents.page-archived")]
public sealed record PageArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
