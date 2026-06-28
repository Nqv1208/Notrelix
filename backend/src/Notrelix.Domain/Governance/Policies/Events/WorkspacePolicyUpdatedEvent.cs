namespace Notrelix.Domain.Governance.Policies.Events;

public sealed record WorkspacePolicyUpdatedEvent(
    Guid WorkspaceId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
