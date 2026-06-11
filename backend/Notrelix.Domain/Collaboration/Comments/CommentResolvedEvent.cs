using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Comments;

public sealed record CommentResolvedEvent(
    Guid WorkspaceId,
    Guid CommentId,
    Guid ResolvedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
