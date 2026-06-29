namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateUpdatedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
