namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-submission-created")]
public sealed record FormSubmissionCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    Guid BoardId,
    Guid? SubmitterUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
