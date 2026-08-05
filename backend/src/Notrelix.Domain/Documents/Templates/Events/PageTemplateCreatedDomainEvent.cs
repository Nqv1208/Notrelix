namespace Notrelix.Domain.Documents.Templates.Events;

[EventName("documents.page-template-created")]
public sealed record PageTemplateCreatedDomainEvent(
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
