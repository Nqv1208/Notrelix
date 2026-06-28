namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateRestoredDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
