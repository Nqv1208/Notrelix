using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserEmailChangedEvent(
    Guid UserId,
    Email OldEmail,
    Email NewEmail,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
