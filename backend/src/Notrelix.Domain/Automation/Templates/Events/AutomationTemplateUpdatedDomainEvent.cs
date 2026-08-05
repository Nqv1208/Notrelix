namespace Notrelix.Domain.Automation.Templates.Events;

[EventName("automation.automation-template-updated")]
public sealed record AutomationTemplateUpdatedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
