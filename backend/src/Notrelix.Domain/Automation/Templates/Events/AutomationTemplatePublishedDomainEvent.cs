namespace Notrelix.Domain.Automation.Templates.Events;

public sealed record AutomationTemplatePublishedDomainEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
