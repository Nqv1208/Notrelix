namespace Notrelix.Domain.Documents.ResourceLinks.Events;

[EventName("documents.resource-link-created")]
public sealed record ResourceLinkCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SourceId,
    Guid TargetId,
    LinkType Type,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
