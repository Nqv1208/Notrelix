namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterCreatedDomainEvent(
    Guid AccountId,
    Guid FilterId,
    Guid WorkspaceId,
    Guid BoardId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt,
    Guid? ViewId = null
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
