namespace Notrelix.Domain.WorkManagement.Labels.Events;

public sealed record LabelCreatedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid LabelId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
