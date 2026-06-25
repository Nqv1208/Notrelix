namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageCreatedDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    string Title,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CreatedBy);
