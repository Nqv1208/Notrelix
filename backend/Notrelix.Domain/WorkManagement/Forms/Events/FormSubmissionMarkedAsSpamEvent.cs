using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSubmissionMarkedAsSpamEvent(
    Guid WorkspaceId,
    Guid SubmissionId,
    Guid FormId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
