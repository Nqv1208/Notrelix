using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Attachments.Events;

public sealed record AttachmentRestoredDomainEvent(
    Guid WorkspaceId,
    Guid AttachmentId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
