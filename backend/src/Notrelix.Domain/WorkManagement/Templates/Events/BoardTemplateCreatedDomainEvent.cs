namespace Notrelix.Domain.WorkManagement.Templates.Events;

public sealed record BoardTemplateCreatedDomainEvent(
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
