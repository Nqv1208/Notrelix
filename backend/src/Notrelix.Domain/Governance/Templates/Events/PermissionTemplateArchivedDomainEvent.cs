namespace Notrelix.Domain.Governance.Templates.Events;

[EventName("governance.permission-template-archived")]
public sealed record PermissionTemplateArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TemplateId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
