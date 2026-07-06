namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record RelationFieldConfiguredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid SourceBoardId,
    Guid TargetBoardId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
