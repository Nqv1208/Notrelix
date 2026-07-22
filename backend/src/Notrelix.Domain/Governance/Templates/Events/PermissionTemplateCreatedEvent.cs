namespace Notrelix.Domain.Governance.Templates.Events;

public sealed record PermissionTemplateCreatedEvent(
    Guid TemplateId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
