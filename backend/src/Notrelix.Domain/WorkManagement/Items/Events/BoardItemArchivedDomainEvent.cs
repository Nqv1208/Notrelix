namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemArchivedDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ArchivedBy);
