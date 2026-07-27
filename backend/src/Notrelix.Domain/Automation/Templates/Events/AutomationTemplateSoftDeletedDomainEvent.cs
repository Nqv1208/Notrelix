namespace Notrelix.Domain.Automation.Templates.Events;

[EventName("automation.automation-template-soft-deleted")]
public sealed record AutomationTemplateSoftDeletedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
