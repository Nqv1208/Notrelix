namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewCreatedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    string Name,
    ViewType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
