namespace Notrelix.Domain.Documents.Templates.Events;

public sealed record PageTemplateCreatedDomainEvent(
    Guid? WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
