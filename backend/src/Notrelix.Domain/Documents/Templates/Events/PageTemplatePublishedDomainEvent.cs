namespace Notrelix.Domain.Documents.Templates.Events;

public sealed record PageTemplatePublishedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
