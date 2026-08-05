namespace Notrelix.Domain.Documents.Pages.Events;

[EventName("documents.page-renamed")]
public sealed record PageRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
