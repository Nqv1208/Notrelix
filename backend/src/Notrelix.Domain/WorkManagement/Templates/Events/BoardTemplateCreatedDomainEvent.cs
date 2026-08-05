namespace Notrelix.Domain.WorkManagement.Templates.Events;

[EventName("work-management.board-template-created")]
public sealed record BoardTemplateCreatedDomainEvent(
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
