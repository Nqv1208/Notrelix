namespace Notrelix.Domain.Automation.Templates.Events;

[EventName("automation.automation-template-created")]
public sealed record AutomationTemplateCreatedDomainEvent(
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
