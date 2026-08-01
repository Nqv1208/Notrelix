namespace Notrelix.Domain.Governance.Templates.Events;

[EventName("governance.workspace-permission-template-created")]
public sealed record WorkspacePermissionTemplateCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
