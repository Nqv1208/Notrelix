namespace Notrelix.Domain.Documents.Pages.Events;

[EventName("documents.page-soft-deleted")]
public sealed record PageSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
