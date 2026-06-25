namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemLabelRemovedDomainEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid LabelId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
