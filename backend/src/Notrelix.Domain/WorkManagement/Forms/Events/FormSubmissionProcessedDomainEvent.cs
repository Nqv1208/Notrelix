namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSubmissionProcessedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    Guid CreatedItemId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
