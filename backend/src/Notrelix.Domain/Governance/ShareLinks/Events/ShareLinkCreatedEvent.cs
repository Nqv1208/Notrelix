namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkCreatedEvent(
    Guid WorkspaceId,
    Guid LinkId,
    ResourceType ResourceType,
    Guid ResourceId,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CreatedBy);
