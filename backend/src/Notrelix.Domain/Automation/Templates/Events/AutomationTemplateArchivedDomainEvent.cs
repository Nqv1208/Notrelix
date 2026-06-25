namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateArchivedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
