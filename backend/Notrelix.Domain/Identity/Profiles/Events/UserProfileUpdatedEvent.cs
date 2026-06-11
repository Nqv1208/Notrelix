using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Profiles.Events;

public sealed record UserProfileUpdatedEvent(
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
