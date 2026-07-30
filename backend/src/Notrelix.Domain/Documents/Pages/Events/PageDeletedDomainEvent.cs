namespace Notrelix.Domain.Documents.Pages.Events;

[EventName("documents.page-deleted")]
public sealed record PageDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
