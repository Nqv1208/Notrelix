namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSubmissionRejectedDomainEvent(
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
