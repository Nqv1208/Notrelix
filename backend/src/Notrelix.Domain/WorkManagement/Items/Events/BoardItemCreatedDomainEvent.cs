namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemCreatedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid ItemId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
