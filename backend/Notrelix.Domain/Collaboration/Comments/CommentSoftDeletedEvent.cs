using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Comments;

public sealed record CommentSoftDeletedEvent(
    Guid WorkspaceId,
    Guid CommentId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
