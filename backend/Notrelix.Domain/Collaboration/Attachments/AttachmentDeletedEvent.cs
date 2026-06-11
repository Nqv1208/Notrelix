using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Attachments;

public sealed record AttachmentDeletedEvent(
    Guid WorkspaceId,
    Guid AttachmentId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
