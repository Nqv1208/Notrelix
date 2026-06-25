namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateRestoredDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
