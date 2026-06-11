using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Mentions;

public sealed record MentionCreatedEvent(
    Guid WorkspaceId,
    Guid MentionId,
    ResourceRef Source,
    Guid MentionedId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
