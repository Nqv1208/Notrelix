using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSubmissionProcessedEvent(
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    Guid CreatedItemId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
