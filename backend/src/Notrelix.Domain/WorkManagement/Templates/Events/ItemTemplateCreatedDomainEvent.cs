namespace Notrelix.Domain.WorkManagement.Templates.Events;

[EventName("work-management.item-template-created")]
public sealed record ItemTemplateCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
