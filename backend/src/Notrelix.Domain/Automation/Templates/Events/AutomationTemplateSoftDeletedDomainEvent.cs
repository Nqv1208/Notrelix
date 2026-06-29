namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplateSoftDeletedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
