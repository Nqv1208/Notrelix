namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplatePublishedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
