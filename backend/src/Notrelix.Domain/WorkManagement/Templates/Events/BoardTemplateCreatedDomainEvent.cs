namespace Notrelix.Domain.WorkManagement.Templates.Events;

public sealed record BoardTemplateCreatedDomainEvent(
    Guid? WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
