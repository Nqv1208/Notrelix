namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-submission-deleted")]
public sealed record FormSubmissionDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
