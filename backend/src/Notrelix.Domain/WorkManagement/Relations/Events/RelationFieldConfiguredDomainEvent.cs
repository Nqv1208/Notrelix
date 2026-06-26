namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record RelationFieldConfiguredDomainEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid SourceBoardId,
    Guid TargetBoardId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
