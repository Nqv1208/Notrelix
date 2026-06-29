namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemLabelAddedDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid LabelId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
