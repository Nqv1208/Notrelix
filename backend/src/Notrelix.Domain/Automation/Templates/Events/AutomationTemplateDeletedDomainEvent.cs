namespace Notrelix.Domain.Automation.Templates.Events;

[EventName("automation.automation-template-deleted")]
public sealed record AutomationTemplateDeletedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
