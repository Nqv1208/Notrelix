namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSubmissionMarkedAsSpamDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
