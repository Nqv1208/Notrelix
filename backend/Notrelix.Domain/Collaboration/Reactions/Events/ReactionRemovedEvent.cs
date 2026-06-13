using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Reactions.Events;

public sealed record ReactionRemovedEvent(
    Guid WorkspaceId,
    Guid ReactionId,
    ResourceRef Target,
    Guid UserId,
    Emoji Emoji,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
