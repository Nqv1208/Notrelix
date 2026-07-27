namespace Notrelix.Domain.WorkManagement.Relations.Events;

[EventName("work-management.relation-field-configured")]
public sealed record RelationFieldConfiguredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid SourceBoardId,
    Guid TargetBoardId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
