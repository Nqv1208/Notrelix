namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-linked")]
public sealed record BoardItemLinkedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SourceItemId,
    ResourceRef Target,
    BoardItemLinkType LinkType,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
