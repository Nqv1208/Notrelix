namespace Notrelix.Domain.Governance.Templates.Events;

[EventName("governance.permission-template-applied")]
public sealed record PermissionTemplateAppliedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TemplateId,
    Guid TargetResourceId,
    Guid AppliedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
