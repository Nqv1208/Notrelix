namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateArchivedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
