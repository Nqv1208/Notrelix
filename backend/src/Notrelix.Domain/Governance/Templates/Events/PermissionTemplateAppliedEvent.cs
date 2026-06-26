namespace Notrelix.Domain.Governance.Templates.Events;

public sealed record PermissionTemplateAppliedEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    Guid TargetResourceId,
    Guid AppliedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, AppliedBy);
