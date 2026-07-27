namespace Notrelix.Domain.Automation.Templates.Events;

[EventName("automation.automation-template-published")]
public sealed record AutomationTemplatePublishedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
