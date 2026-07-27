namespace Notrelix.Domain.Automation.Templates.Events;

[EventName("automation.automation-template-restored")]
public sealed record AutomationTemplateRestoredDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
