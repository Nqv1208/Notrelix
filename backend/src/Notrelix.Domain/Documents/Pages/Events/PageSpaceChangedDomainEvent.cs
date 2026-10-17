namespace Notrelix.Domain.Documents.Pages.Events;

[EventName("documents.page-space-changed")]
public sealed record PageSpaceChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    Guid? OldSpaceId,
    Guid? NewSpaceId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
