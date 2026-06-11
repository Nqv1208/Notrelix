using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Attachments;

public sealed record AttachmentCreatedEvent(
    Guid WorkspaceId,
    Guid AttachmentId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
