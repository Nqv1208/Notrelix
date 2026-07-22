namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardItemIdentityGeneratedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    long SequenceNumber,
    string ItemKey,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
