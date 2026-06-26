namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageMovedDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    Guid? OldParentId,
    Guid? NewParentId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
