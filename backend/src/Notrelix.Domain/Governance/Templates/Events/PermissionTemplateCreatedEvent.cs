namespace Notrelix.Domain.Governance.Templates.Events;

public sealed record PermissionTemplateCreatedEvent(
    Guid? WorkspaceId,
    Guid TemplateId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CreatedBy);
