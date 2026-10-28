namespace Notrelix.Domain.WorkManagement.Templates.Events;

[EventName("work-management.board-template-created")]
public sealed record BoardTemplateCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
