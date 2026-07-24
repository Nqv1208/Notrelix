namespace Notrelix.Domain.Documents.Templates.Events;

[EventName("documents.page-template-archived")]
public sealed record PageTemplateArchivedDomainEvent(
    Guid TemplateId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);