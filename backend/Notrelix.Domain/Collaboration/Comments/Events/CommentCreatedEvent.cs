using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Comments.Events;

public sealed record CommentCreatedEvent(
    Guid WorkspaceId,
    Guid CommentId,
    ResourceRef Target,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
