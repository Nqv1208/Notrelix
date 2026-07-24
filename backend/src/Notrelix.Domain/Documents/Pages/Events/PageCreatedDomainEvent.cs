namespace Notrelix.Domain.Documents.Pages.Events;

[EventName("documents.page-created")]
public sealed record PageCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    string Title,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
