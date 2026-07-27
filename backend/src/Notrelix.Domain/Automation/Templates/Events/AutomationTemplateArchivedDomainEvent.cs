namespace Notrelix.Domain.Automation.Templates.Events;

[EventName("automation.automation-template-archived")]
public sealed record AutomationTemplateArchivedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
