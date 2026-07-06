namespace Notrelix.Domain.Governance.Templates.Events;

public sealed record PermissionTemplateAppliedEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TemplateId,
    Guid TargetResourceId,
    Guid AppliedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, AppliedBy);
