namespace Notrelix.Domain.Governance.Policies.Events;

[EventName("governance.workspace-policy-updated")]
public sealed record WorkspacePolicyUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
