namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateCreatedDomainEvent(
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
