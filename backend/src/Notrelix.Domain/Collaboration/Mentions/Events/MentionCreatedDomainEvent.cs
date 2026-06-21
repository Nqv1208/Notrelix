using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Mentions.Events;

public sealed record MentionCreatedDomainEvent(
    Guid WorkspaceId,
    Guid MentionId,
    ResourceRef Source,
    Guid MentionedId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
