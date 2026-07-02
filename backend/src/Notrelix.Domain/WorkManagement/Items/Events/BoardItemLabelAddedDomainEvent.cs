namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemLabelAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid LabelId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
