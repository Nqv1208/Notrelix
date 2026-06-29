namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSubmissionProcessedDomainEvent(
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    Guid CreatedItemId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
