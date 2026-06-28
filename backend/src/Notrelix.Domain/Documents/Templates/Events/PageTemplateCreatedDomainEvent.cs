namespace Notrelix.Domain.Documents.Templates.Events;

public sealed record PageTemplateCreatedDomainEvent(
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
