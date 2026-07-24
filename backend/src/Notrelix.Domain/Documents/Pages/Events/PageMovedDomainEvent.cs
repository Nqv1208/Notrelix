namespace Notrelix.Domain.Documents.Pages.Events;

[EventName("documents.page-moved")]
public sealed record PageMovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    Guid? OldParentId,
    Guid? NewParentId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
