namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemLinkedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SourceItemId,
    ResourceRef Target,
    BoardItemLinkType LinkType,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
