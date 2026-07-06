namespace Notrelix.Domain.WorkManagement.Labels.Events;

public sealed record LabelCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid LabelId,
    string Name,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
