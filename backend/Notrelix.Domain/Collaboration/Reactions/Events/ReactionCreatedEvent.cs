using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Reactions.Events;

public sealed record ReactionCreatedEvent(
    Guid WorkspaceId,
    Guid ReactionId,
    ResourceRef Target,
    Guid UserId,
    Emoji Emoji,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
