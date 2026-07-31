namespace Notrelix.Domain.Governance.Templates.Events;

[EventName("governance.permission-template-created")]
public sealed record SystemPermissionTemplateCreatedDomainEvent(
    Guid TemplateId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
