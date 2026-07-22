namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
