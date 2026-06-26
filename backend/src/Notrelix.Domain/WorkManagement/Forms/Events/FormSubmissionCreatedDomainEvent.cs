namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSubmissionCreatedDomainEvent(
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    Guid BoardId,
    Guid? SubmitterUserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, SubmitterUserId);
