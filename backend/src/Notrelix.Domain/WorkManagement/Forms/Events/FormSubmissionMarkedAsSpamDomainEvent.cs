namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-submission-marked-as-spam")]
public sealed record FormSubmissionMarkedAsSpamDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
