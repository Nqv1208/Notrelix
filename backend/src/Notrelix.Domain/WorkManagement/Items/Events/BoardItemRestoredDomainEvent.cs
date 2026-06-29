namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemRestoredDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
