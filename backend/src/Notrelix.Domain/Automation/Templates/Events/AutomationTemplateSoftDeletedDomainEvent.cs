namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateSoftDeletedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
