namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-submission-rejected")]
public sealed record FormSubmissionRejectedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
