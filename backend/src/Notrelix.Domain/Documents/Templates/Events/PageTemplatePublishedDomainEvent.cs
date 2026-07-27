namespace Notrelix.Domain.Documents.Templates.Events;

[EventName("documents.page-template-published")]
public sealed record PageTemplatePublishedDomainEvent(
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
