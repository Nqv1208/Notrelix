namespace Notrelix.Domain.WorkManagement.Templates.Events;

public sealed record ItemTemplateCreatedDomainEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
