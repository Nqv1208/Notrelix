namespace Notrelix.Domain.Governance.Policies.Events;

public sealed record WorkspacePolicyUpdatedEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
